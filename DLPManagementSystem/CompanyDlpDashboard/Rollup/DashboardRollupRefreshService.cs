using System.Data;
using DLPManagementSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.CompanyDlpDashboard.Rollup;

// dashboard-rollup: the single place that writes DashboardHourlyRollup/DashboardEmployeeDailyRollup/
// DashboardRollupWatermark. Used by both DashboardRollupRefreshWorker (the regular rolling-window
// recompute) and the manual backfill admin endpoint (DashboardRollupAdminController) - same
// aggregation SQL, same delete-then-insert-in-a-short-transaction write pattern, same distributed
// lock, so there is exactly one place that can get any of this wrong.
//
// Concurrency/visibility design (see the dashboard-rollup plan's item 3):
//   - sp_getapplock (session-scoped, on a dedicated connection held for the whole call) ensures at
//     most one instance/thread is ever writing the rollup tables at once - more than one app
//     instance can legitimately be running during an app-pool recycle, and without this they could
//     both recompute the same window and double-write (not double-count, since each write deletes
//     its own range first, but still wasted, racy work).
//   - READ_COMMITTED_SNAPSHOT is OFF on this database (checked directly - see the dashboard-rollup
//     plan) and this feature does not turn it on (a database-wide isolation change is out of
//     scope for an additive feature). Instead: the (potentially slow) aggregation SELECT against
//     raw AuditEvents runs with NO open transaction at all, into an in-memory list; only the
//     delete-then-insert of that already-computed, bounded-size result set runs inside a
//     transaction, and that transaction is short by construction (no raw-table scan inside it). A
//     dashboard reader under the database's default READ COMMITTED isolation therefore either sees
//     the complete old rows (before the transaction starts) or the complete new rows (after it
//     commits) - at worst it blocks briefly waiting for that short transaction's locks to release,
//     it never sees a partially-deleted/partially-inserted bucket.
public sealed class DashboardRollupRefreshService : IDashboardRollupRefreshService
{
    private const string ApplockResource = "DashboardRollupRefresh";

    private readonly string _connectionString;
    private readonly DashboardRollupOptions _options;
    private readonly ILogger<DashboardRollupRefreshService> _logger;

    public DashboardRollupRefreshService(
        IConfiguration configuration,
        IOptions<DashboardRollupOptions> options,
        ILogger<DashboardRollupRefreshService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public async Task<DashboardRollupRefreshResult> RefreshRecentWindowAsync(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await using var lockConnection = new SqlConnection(_connectionString);
        await lockConnection.OpenAsync(cancellationToken);

        if (!await TryAcquireApplockAsync(lockConnection, _options.DistributedLockTimeoutMilliseconds, cancellationToken))
        {
            // Normal/expected when more than one instance is running - not an error.
            _logger.LogDebug("Dashboard rollup refresh: another instance already holds the lock; skipping this tick.");
            return new DashboardRollupRefreshResult(false, 0, 0, 0, stopwatch.Elapsed);
        }

        try
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var hourlyTo = FloorToHour(nowUtc);
            var hourlyFrom = hourlyTo.AddHours(-Math.Max(1, _options.LateEventLookbackHours));
            var employeeTo = FloorToDay(nowUtc);
            var employeeFrom = employeeTo.AddDays(-Math.Max(1, _options.EffectiveEmployeeRollupRecomputeDays));

            var organizationIds = await GetActiveOrganizationIdsAsync(cancellationToken);

            long hourlyRowsWritten = 0;
            long employeeRowsWritten = 0;

            foreach (var organizationId in organizationIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    hourlyRowsWritten += await RefreshHourlyAsync(
                        organizationId, DashboardRollupTimeBasis.Received, hourlyFrom, hourlyTo, cancellationToken);
                    hourlyRowsWritten += await RefreshHourlyAsync(
                        organizationId, DashboardRollupTimeBasis.Occurred, hourlyFrom, hourlyTo, cancellationToken);
                    employeeRowsWritten += await RefreshEmployeeDailyAsync(
                        organizationId, employeeFrom, employeeTo, cancellationToken);
                    employeeRowsWritten += await MaybeRunEmployeeWideSweepAsync(
                        organizationId, nowUtc, employeeTo, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One organization's failure (a transient SQL error, a timeout) must not stop
                    // the rest from refreshing, and must not crash the worker - logged and skipped,
                    // picked up again next tick.
                    _logger.LogError(ex,
                        "Dashboard rollup refresh failed for organization {OrganizationId}; will retry next cycle.",
                        organizationId);
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Dashboard rollup refresh cycle complete: {OrgCount} organizations, {HourlyRows} hourly rows, " +
                "{EmployeeRows} employee-daily rows written in {ElapsedMs}ms.",
                organizationIds.Count, hourlyRowsWritten, employeeRowsWritten, stopwatch.ElapsedMilliseconds);

            return new DashboardRollupRefreshResult(true, organizationIds.Count, hourlyRowsWritten, employeeRowsWritten, stopwatch.Elapsed);
        }
        finally
        {
            // Session-scoped applock - releasing explicitly here (rather than only relying on the
            // connection closing a few lines later) so the lock is gone the instant this method
            // returns, not whenever the connection object happens to finish disposing.
            await TryReleaseApplockAsync(lockConnection, cancellationToken);
        }
    }

    public async Task<DashboardRollupBackfillChunkResult> BackfillOneChunkAsync(
        Guid organizationId,
        DashboardRollupKindSelector kind,
        CancellationToken cancellationToken)
    {
        await using var lockConnection = new SqlConnection(_connectionString);
        await lockConnection.OpenAsync(cancellationToken);

        // A deliberate, manual admin action is worth waiting a few seconds for the regular cycle
        // (or another backfill call) to finish, rather than instantly giving up like the regular
        // worker's own tick does.
        if (!await TryAcquireApplockAsync(lockConnection, 5000, cancellationToken))
        {
            throw new InvalidOperationException(
                "Could not acquire the dashboard rollup lock within 5 seconds - another refresh or backfill is in progress. Try again shortly.");
        }

        try
        {
            var rollupKind = kind switch
            {
                DashboardRollupKindSelector.HourlyReceived => DashboardRollupKind.HourlyReceived,
                DashboardRollupKindSelector.HourlyOccurred => DashboardRollupKind.HourlyOccurred,
                DashboardRollupKindSelector.EmployeeDaily => DashboardRollupKind.EmployeeDaily,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };

            await using var db = await OpenDbContextAsync(cancellationToken);

            var watermark = await db.DashboardRollupWatermarks.FindAsync(
                new object?[] { organizationId, rollupKind.ToString() }, cancellationToken);

            var nowUtc = DateTimeOffset.UtcNow;
            var chunkTo = watermark?.LastComputedThroughUtc
                ?? (kind == DashboardRollupKindSelector.EmployeeDaily ? FloorToDay(nowUtc) : FloorToHour(nowUtc));

            var earliestUtc = await GetEarliestTimestampAsync(organizationId, kind, cancellationToken);

            if (earliestUtc is null || chunkTo <= earliestUtc)
            {
                // Nothing (left) to backfill for this org/kind.
                return new DashboardRollupBackfillChunkResult(organizationId, rollupKind.ToString(), false, chunkTo, 0);
            }

            var chunkDays = Math.Max(1, _options.BackfillChunkDays);
            var chunkFrom = kind == DashboardRollupKindSelector.EmployeeDaily
                ? chunkTo.AddDays(-chunkDays)
                : chunkTo.AddHours(-chunkDays * 24);

            // Never aggregate further back than the organization's actual earliest event - the
            // final chunk is whatever's left, not a full chunkDays-wide window past it.
            if (chunkFrom < earliestUtc.Value)
            {
                chunkFrom = FloorForKind(earliestUtc.Value, kind);
            }

            long rowsWritten = kind switch
            {
                DashboardRollupKindSelector.HourlyReceived =>
                    await RefreshHourlyAsync(organizationId, DashboardRollupTimeBasis.Received, chunkFrom, chunkTo, cancellationToken, isBackfill: true),
                DashboardRollupKindSelector.HourlyOccurred =>
                    await RefreshHourlyAsync(organizationId, DashboardRollupTimeBasis.Occurred, chunkFrom, chunkTo, cancellationToken, isBackfill: true),
                _ =>
                    await RefreshEmployeeDailyAsync(organizationId, chunkFrom, chunkTo, cancellationToken, isBackfill: true)
            };

            await UpsertWatermarkAsync(organizationId, rollupKind, chunkFrom, cancellationToken);

            var moreRemaining = chunkFrom > earliestUtc.Value;

            _logger.LogInformation(
                "Dashboard rollup backfill: organization {OrganizationId}, kind {Kind}, chunk [{From:o}, {To:o}), " +
                "{Rows} rows written, moreRemaining={MoreRemaining}.",
                organizationId, rollupKind, chunkFrom, chunkTo, rowsWritten, moreRemaining);

            return new DashboardRollupBackfillChunkResult(organizationId, rollupKind.ToString(), moreRemaining, chunkFrom, rowsWritten);
        }
        finally
        {
            await TryReleaseApplockAsync(lockConnection, cancellationToken);
        }
    }

    // --- Hourly (Received/Occurred) -----------------------------------------------------------

    private async Task<long> RefreshHourlyAsync(
        Guid organizationId,
        DashboardRollupTimeBasis basis,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken,
        bool isBackfill = false)
    {
        var timeColumn = basis == DashboardRollupTimeBasis.Received ? "ReceivedAtUtc" : "OccurredAtUtc";
        var commandTimeout = isBackfill ? _options.BackfillCommandTimeoutSeconds : _options.RefreshCommandTimeoutSeconds;

        var sql = $@"
SELECT
    DATEADD(HOUR, DATEDIFF(HOUR, 0, ae.{timeColumn}), 0) AS BucketStartUtc,
    ae.DecisionId,
    ae.ActionKey,
    ae.ReasonCodeId,
    COUNT_BIG(1) AS EventCount,
    MAX(ae.ReceivedAtUtc) AS LastReceivedAtUtc,
    MAX(ae.OccurredAtUtc) AS LastOccurredAtUtc
FROM dbo.AuditEvents ae
WHERE ae.OrganizationId = @OrganizationId
  AND ae.{timeColumn} >= @FromUtc AND ae.{timeColumn} < @ToUtc
GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, ae.{timeColumn}), 0), ae.DecisionId, ae.ActionKey, ae.ReasonCodeId;";

        var rows = new List<DashboardHourlyRollup>();
        var updatedAtUtc = DateTimeOffset.UtcNow;

        await using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = commandTimeout;
            command.Parameters.Add(new SqlParameter("@OrganizationId", SqlDbType.UniqueIdentifier) { Value = organizationId });
            command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTimeOffset) { Value = fromUtc });
            command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTimeOffset) { Value = toUtc });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new DashboardHourlyRollup
                {
                    OrganizationId = organizationId,
                    TimeBasis = basis,
                    BucketStartUtc = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc), TimeSpan.Zero),
                    DecisionId = reader.GetInt32(1),
                    ActionKey = reader.GetString(2),
                    ReasonCodeId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    EventCount = reader.GetInt64(4),
                    LastReceivedAtUtc = reader.GetFieldValue<DateTimeOffset>(5),
                    LastOccurredAtUtc = reader.GetFieldValue<DateTimeOffset>(6),
                    UpdatedAtUtc = updatedAtUtc
                });
            }
        }

        await using var db = await OpenDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.DashboardHourlyRollups
                .Where(x => x.OrganizationId == organizationId && x.TimeBasis == basis
                    && x.BucketStartUtc >= fromUtc && x.BucketStartUtc < toUtc)
                .ExecuteDeleteAsync(cancellationToken);

            if (rows.Count > 0)
            {
                db.DashboardHourlyRollups.AddRange(rows);
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (!isBackfill)
        {
            await UpsertWatermarkAsync(
                organizationId,
                basis == DashboardRollupTimeBasis.Received ? DashboardRollupKind.HourlyReceived : DashboardRollupKind.HourlyOccurred,
                fromUtc,
                cancellationToken);
        }

        return rows.Count;
    }

    // --- EmployeeDaily -----------------------------------------------------------------------

    private async Task<long> RefreshEmployeeDailyAsync(
        Guid organizationId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken,
        bool isBackfill = false)
    {
        var commandTimeout = isBackfill ? _options.BackfillCommandTimeoutSeconds : _options.RefreshCommandTimeoutSeconds;

        const string sql = @"
SELECT
    DATEADD(DAY, DATEDIFF(DAY, 0, ae.OccurredAtUtc), 0) AS BucketDateUtc,
    ae.EmployeeId,
    ae.ActionKey,
    ae.DecisionId,
    COUNT_BIG(1) AS EventCount
FROM dbo.AuditEvents ae
WHERE ae.OrganizationId = @OrganizationId
  AND ae.EmployeeId IS NOT NULL
  AND ae.OccurredAtUtc >= @FromUtc AND ae.OccurredAtUtc < @ToUtc
GROUP BY DATEADD(DAY, DATEDIFF(DAY, 0, ae.OccurredAtUtc), 0), ae.EmployeeId, ae.ActionKey, ae.DecisionId;";

        var rows = new List<DashboardEmployeeDailyRollup>();
        var updatedAtUtc = DateTimeOffset.UtcNow;

        await using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = commandTimeout;
            command.Parameters.Add(new SqlParameter("@OrganizationId", SqlDbType.UniqueIdentifier) { Value = organizationId });
            command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTimeOffset) { Value = fromUtc });
            command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTimeOffset) { Value = toUtc });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new DashboardEmployeeDailyRollup
                {
                    OrganizationId = organizationId,
                    BucketDateUtc = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc), TimeSpan.Zero),
                    EmployeeId = reader.GetGuid(1),
                    ActionKey = reader.GetString(2),
                    DecisionId = reader.GetInt32(3),
                    EventCount = reader.GetInt64(4),
                    UpdatedAtUtc = updatedAtUtc
                });
            }
        }

        await using var db = await OpenDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.DashboardEmployeeDailyRollups
                .Where(x => x.OrganizationId == organizationId && x.BucketDateUtc >= fromUtc && x.BucketDateUtc < toUtc)
                .ExecuteDeleteAsync(cancellationToken);

            if (rows.Count > 0)
            {
                db.DashboardEmployeeDailyRollups.AddRange(rows);
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        if (!isBackfill)
        {
            await UpsertWatermarkAsync(organizationId, DashboardRollupKind.EmployeeDaily, fromUtc, cancellationToken);
        }

        return rows.Count;
    }

    // --- Watermark -----------------------------------------------------------------------------

    // Always takes MIN(existing, candidate) - the watermark only ever moves further into the past
    // (more history proven complete) or initializes if it doesn't exist yet, never forward. The
    // regular refresh cycle's own calls only ever hit the "initialize" path in practice (its
    // window is fixed, so candidate never ends up older than an existing watermark); backfill's
    // entire purpose is the "extend backward" path.
    private async Task UpsertWatermarkAsync(
        Guid organizationId,
        DashboardRollupKind kind,
        DateTimeOffset candidateWatermarkUtc,
        CancellationToken cancellationToken)
    {
        await using var db = await OpenDbContextAsync(cancellationToken);
        var kindName = kind.ToString();
        var nowUtc = DateTimeOffset.UtcNow;

        var existing = await db.DashboardRollupWatermarks.FindAsync(new object?[] { organizationId, kindName }, cancellationToken);

        if (existing is null)
        {
            db.DashboardRollupWatermarks.Add(new DashboardRollupWatermark
            {
                OrganizationId = organizationId,
                RollupKind = kindName,
                LastComputedThroughUtc = candidateWatermarkUtc,
                UpdatedAtUtc = nowUtc
            });
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        // Watermark is monotonically non-increasing - it only ever moves further into the past
        // (more history proven complete), never forward, regardless of which caller is updating it.
        if (candidateWatermarkUtc < existing.LastComputedThroughUtc)
        {
            existing.LastComputedThroughUtc = candidateWatermarkUtc;
            existing.UpdatedAtUtc = nowUtc;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    // --- Daily wide sweep (EmployeeDaily safety net) ---------------------------------------------
    //
    // A mobile device can be offline for longer than LateEventLookbackHours and only send its
    // backlog once reconnected - the regular per-tick EmployeeDaily window (bounded by
    // EffectiveEmployeeRollupRecomputeDays) deliberately never reaches that far back, by design
    // (see DashboardRollupOptions). This sweep is the automatic safety net for that case: once per
    // UTC day, at EmployeeRollupWideSweepHourUtc, it re-aggregates EmployeeRollupWideSweepDays
    // instead of the narrow window. It runs from inside RefreshRecentWindowAsync, so it always
    // holds the same sp_getapplock as the regular cycle - never concurrently with another
    // instance's cycle or with a manual backfill call (BackfillOneChunkAsync waits on the very
    // same lock).
    private async Task<long> MaybeRunEmployeeWideSweepAsync(
        Guid organizationId, DateTimeOffset nowUtc, DateTimeOffset employeeTo, CancellationToken cancellationToken)
    {
        if (!_options.EmployeeRollupWideSweepEnabled)
        {
            return 0;
        }

        if (nowUtc.UtcDateTime.Hour != Math.Clamp(_options.EmployeeRollupWideSweepHourUtc, 0, 23))
        {
            return 0;
        }

        var kindName = DashboardRollupKind.EmployeeDaily.ToString();
        await using (var db = await OpenDbContextAsync(cancellationToken))
        {
            var watermark = await db.DashboardRollupWatermarks.FindAsync(new object?[] { organizationId, kindName }, cancellationToken);
            if (watermark?.LastWideSweepUtc is { } lastSweepUtc && lastSweepUtc.UtcDateTime.Date == nowUtc.UtcDateTime.Date)
            {
                // Already ran today for this organization - at most once per UTC day, even if the
                // regular tick happens to fall in this hour more than once (clock changes, a
                // restart that re-enters the same hour, etc).
                return 0;
            }
        }

        var wideFrom = employeeTo.AddDays(-Math.Max(1, _options.EmployeeRollupWideSweepDays));

        var rowsWritten = await RefreshEmployeeDailyAsync(organizationId, wideFrom, employeeTo, cancellationToken, isBackfill: true);

        await UpsertWatermarkAsync(organizationId, DashboardRollupKind.EmployeeDaily, wideFrom, cancellationToken);
        await MarkWideSweepRanAsync(organizationId, kindName, nowUtc, cancellationToken);

        _logger.LogInformation(
            "Dashboard rollup daily wide sweep: organization {OrganizationId}, window [{From:o}, {To:o}), {Rows} rows written.",
            organizationId, wideFrom, employeeTo, rowsWritten);

        return rowsWritten;
    }

    private async Task MarkWideSweepRanAsync(Guid organizationId, string kindName, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        await using var db = await OpenDbContextAsync(cancellationToken);
        var watermark = await db.DashboardRollupWatermarks.FindAsync(new object?[] { organizationId, kindName }, cancellationToken);
        if (watermark is null)
        {
            // UpsertWatermarkAsync (called just before this, in the same method) always creates
            // the row if it didn't already exist, so this should never actually happen - a
            // defensive no-op rather than throwing from what is otherwise just a timestamp update.
            return;
        }

        watermark.LastWideSweepUtc = nowUtc;
        watermark.UpdatedAtUtc = nowUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    // --- Helpers ---------------------------------------------------------------------------------

    private async Task<List<Guid>> GetActiveOrganizationIdsAsync(CancellationToken cancellationToken)
    {
        await using var db = await OpenDbContextAsync(cancellationToken);
        return await db.Organizations.Where(x => x.IsActive).Select(x => x.Id).ToListAsync(cancellationToken);
    }

    private async Task<DateTimeOffset?> GetEarliestTimestampAsync(
        Guid organizationId, DashboardRollupKindSelector kind, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        // HourlyOccurred aggregates every AuditEvents row for the organization (see
        // RefreshHourlyAsync's SQL - no EmployeeId filter at all); only EmployeeDaily is restricted
        // to employee-tagged events. The earliest-timestamp lookup must mirror each rollup's own
        // aggregation filter exactly, or backfill can wrongly report "nothing to backfill" for an
        // organization whose events have no EmployeeId.
        command.CommandText = kind switch
        {
            DashboardRollupKindSelector.HourlyReceived =>
                "SELECT MIN(ReceivedAtUtc) FROM dbo.AuditEvents WHERE OrganizationId = @OrganizationId;",
            DashboardRollupKindSelector.HourlyOccurred =>
                "SELECT MIN(OccurredAtUtc) FROM dbo.AuditEvents WHERE OrganizationId = @OrganizationId;",
            DashboardRollupKindSelector.EmployeeDaily =>
                "SELECT MIN(OccurredAtUtc) FROM dbo.AuditEvents WHERE OrganizationId = @OrganizationId AND EmployeeId IS NOT NULL;",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        command.CommandTimeout = _options.BackfillCommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@OrganizationId", SqlDbType.UniqueIdentifier) { Value = organizationId });

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? null : (DateTimeOffset)result;
    }

    private static DateTimeOffset FloorForKind(DateTimeOffset value, DashboardRollupKindSelector kind) =>
        kind == DashboardRollupKindSelector.EmployeeDaily ? FloorToDay(value) : FloorToHour(value);

    private static DateTimeOffset FloorToHour(DateTimeOffset value)
    {
        var utc = value.UtcDateTime;
        return new DateTimeOffset(new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
    }

    private static DateTimeOffset FloorToDay(DateTimeOffset value)
    {
        var utc = value.UtcDateTime;
        return new DateTimeOffset(new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
    }

    private async Task<DLPSystemContext> OpenDbContextAsync(CancellationToken cancellationToken)
    {
        var db = new DLPSystemContext(new DbContextOptionsBuilder<DLPSystemContext>()
            .UseSqlServer(_connectionString, o => o.CommandTimeout(_options.RefreshCommandTimeoutSeconds))
            .Options);
        await db.Database.OpenConnectionAsync(cancellationToken);
        return db;
    }

    private static async Task<bool> TryAcquireApplockAsync(SqlConnection connection, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "sp_getapplock";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = ApplockResource });
        command.Parameters.Add(new SqlParameter("@LockMode", SqlDbType.VarChar, 32) { Value = "Exclusive" });
        command.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Session" });
        command.Parameters.Add(new SqlParameter("@LockTimeout", SqlDbType.Int) { Value = timeoutMilliseconds });
        var returnValue = new SqlParameter("@ReturnValue", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
        command.Parameters.Add(returnValue);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return (int)returnValue.Value >= 0;
    }

    private static async Task TryReleaseApplockAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "sp_releaseapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = ApplockResource });
            command.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Session" });
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Best-effort - the connection is about to be disposed either way, which releases any
            // session-scoped applock regardless.
        }
    }
}
