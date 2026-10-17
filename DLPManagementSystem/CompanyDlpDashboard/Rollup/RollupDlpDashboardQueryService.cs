using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.CompanyDlpDashboard.Rollup;

// dashboard-rollup: reads kpis/breakdown/weekly-trend/risk from DashboardHourlyRollup/
// DashboardEmployeeDailyRollup instead of raw AuditEvents - only ever used when
// DashboardRollup:UseRollup is true (see RollupAwareDlpDashboardQueryService, the decorator that
// chooses between this and the raw SqlDlpDashboardQueryService).
//
// Window alignment (see the dashboard-rollup plan's item 2): the rollup's own grain is the hour
// (DashboardHourlyRollup) or the day (DashboardEmployeeDailyRollup) - every window this class
// computes is floored to that grain BEFORE querying, which is why its numbers can differ from the
// raw path's by up to that grain's width (≤1h for kpis/breakdown/weekly-trend, ≤1 day for risk).
// This is a deliberate, documented consequence of the rollup's grain (see
// DASHBOARD_ENDPOINTS.md's "Rollup" section), not a bug - the equivalence tests pick an already
// grain-aligned "now" specifically so the comparison itself has zero slack.
public sealed class RollupDlpDashboardQueryService : IRollupDlpDashboardQueryService
{
    // Mirrors SqlDlpDashboardQueryService's own risk constants exactly - the two paths must agree
    // on what "High"/"Medium" mean regardless of which one is actually serving a request.
    private const int RiskHighThreshold = 70;
    private const int RiskMediumThreshold = 40;
    private const int TopRiskyUserCount = 10;

    private readonly string _connectionString;
    private readonly DashboardRollupOptions _options;

    public RollupDlpDashboardQueryService(IConfiguration configuration, IOptions<DashboardRollupOptions> options)
    {
        _options = options.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    public async Task<DlpKpisDto> GetKpisAsync(Guid organizationId, DashboardRange range, CancellationToken cancellationToken)
    {
        var (from, to) = AlignedHourlyWindow(range.Kind, DateTimeOffset.UtcNow);
        var hourly = to - from <= TimeSpan.FromHours(24);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        DlpKpiTotalsDto currentTotals;
        IReadOnlyList<DlpDashboardSeriesPointDto> series;

        var batchSql = TotalsSql + BuildRollupSeriesSql(hourly);

        await using (var command = CreateCommand(connection, batchSql, organizationId, from, to))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            currentTotals = await ReadLeanTotalsAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            var byBucket = await SqlDlpDashboardQueryService.ReadSeriesRowsAsync(reader, cancellationToken);
            series = SqlDlpDashboardQueryService.ZeroFillSeries(byBucket, hourly, from, to);
        }

        var previousDuration = to - from;
        var previousTotals = await GetTotalsAsync(connection, organizationId, from - previousDuration, from, cancellationToken);

        return new DlpKpisDto(range.Key, from, to, currentTotals, previousTotals, series);
    }

    public async Task<DlpBreakdownDto> GetBreakdownAsync(Guid organizationId, DashboardRange range, CancellationToken cancellationToken)
    {
        var (from, to) = AlignedHourlyWindow(range.Kind, DateTimeOffset.UtcNow);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var batchSql = ActionBreakdownTop8Sql + ReasonBreakdownTop8Sql + ChannelBreakdownSql;
        await using var command = CreateCommand(connection, batchSql, organizationId, from, to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var actionBreakdown = await ReadActionBreakdownAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var reasonBreakdown = await ReadReasonBreakdownAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var channelBreakdown = await ReadChannelBreakdownAsync(reader, cancellationToken);

        return new DlpBreakdownDto(actionBreakdown, reasonBreakdown, channelBreakdown);
    }

    public async Task<IReadOnlyList<DlpWeeklyTrendPointDto>> GetWeeklyTrendAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var nowFloorHour = FloorToHour(DateTimeOffset.UtcNow);
        var weeklyFrom = nowFloorHour.AddDays(-28);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT
    DATEDIFF(SECOND, @WeeklyFromUtc, r.BucketStartUtc) / 604800 AS WeekIndex,
    SUM(CASE WHEN ad.Name = N'Block' THEN r.EventCount ELSE 0 END) AS BlockedCount,
    SUM(CASE WHEN ad.Name IN (N'Block', N'AuditOnly', N'Audit') THEN r.EventCount ELSE 0 END) AS ViolationCount
FROM dbo.DashboardHourlyRollups r
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = r.DecisionId
WHERE r.OrganizationId = @OrganizationId
  AND r.TimeBasis = 1
  AND r.BucketStartUtc >= @WeeklyFromUtc AND r.BucketStartUtc < @WeeklyToUtc
GROUP BY DATEDIFF(SECOND, @WeeklyFromUtc, r.BucketStartUtc) / 604800;";
        command.CommandTimeout = _options.RefreshCommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@OrganizationId", SqlDbType.UniqueIdentifier) { Value = organizationId });
        command.Parameters.Add(new SqlParameter("@WeeklyFromUtc", SqlDbType.DateTimeOffset) { Value = weeklyFrom });
        command.Parameters.Add(new SqlParameter("@WeeklyToUtc", SqlDbType.DateTimeOffset) { Value = nowFloorHour });

        var byWeekIndex = new Dictionary<int, (long Blocked, long Violations)>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var weekIndex = SqlDlpDashboardQueryService.GetInt(reader, "WeekIndex");
                byWeekIndex[weekIndex] = (SqlDlpDashboardQueryService.GetLong(reader, "BlockedCount"), SqlDlpDashboardQueryService.GetLong(reader, "ViolationCount"));
            }
        }

        var points = new List<DlpWeeklyTrendPointDto>();
        for (var weekIndex = 0; weekIndex < 4; weekIndex++)
        {
            var bucketStart = weeklyFrom.AddDays(7 * weekIndex);
            var (blocked, violations) = byWeekIndex.TryGetValue(weekIndex, out var counts) ? counts : (0, 0);
            points.Add(new DlpWeeklyTrendPointDto(bucketStart, blocked, violations));
        }

        return points;
    }

    public async Task<DlpRiskDto> GetRiskAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var todayFloorDay = FloorToDay(DateTimeOffset.UtcNow);
        var riskFrom = todayFloorDay.AddDays(-Math.Max(1, _options.RiskLookbackDays));

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = RiskBlockCountSql + RiskFactorSql;
        command.CommandTimeout = _options.RefreshCommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@OrganizationId", SqlDbType.UniqueIdentifier) { Value = organizationId });
        command.Parameters.Add(new SqlParameter("@RiskFromUtc", SqlDbType.DateTimeOffset) { Value = riskFrom });
        command.Parameters.Add(new SqlParameter("@RiskToUtc", SqlDbType.DateTimeOffset) { Value = todayFloorDay });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var employeeRows = await ReadRiskBlockCountsAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var factorsByEmployee = await ReadRiskFactorsAsync(reader, cancellationToken);

        var (topRiskyUsers, highRiskUserCount, riskByDepartment) =
            SqlDlpDashboardQueryService.ComputeRiskScores(employeeRows, factorsByEmployee);

        return new DlpRiskDto(topRiskyUsers, riskByDepartment, highRiskUserCount);
    }

    // --- Window alignment --------------------------------------------------------------------

    internal static (DateTimeOffset From, DateTimeOffset To) AlignedHourlyWindow(DashboardRangeKind kind, DateTimeOffset nowUtc)
    {
        var to = FloorToHour(nowUtc);
        var duration = kind switch
        {
            DashboardRangeKind.SixHours => TimeSpan.FromHours(6),
            DashboardRangeKind.TwentyFourHours => TimeSpan.FromHours(24),
            DashboardRangeKind.SevenDays => TimeSpan.FromDays(7),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return (to - duration, to);
    }

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

    // Same output column names SqlDlpDashboardQueryService.ReadSeriesRowsAsync already expects
    // (BucketStart/Total/Allowed/Blocked/AuditOnly/Errors) - written out explicitly against the
    // rollup table rather than string-patching BuildSeriesSql's raw-table text, which would be
    // fragile (a silent no-op if that text's shape ever changes) for no real savings.
    private static string BuildRollupSeriesSql(bool hourly)
    {
        // ReadSeriesRowsAsync (reused from SqlDlpDashboardQueryService) reads this column with
        // SqlDataReader.GetDateTime, which throws InvalidCastException against a raw
        // datetimeoffset column - the raw path's own BuildSeriesSql avoids this the same way
        // (CAST(... AS datetime2) before any DATEADD/DATEDIFF), so this must match it exactly: the
        // hourly case needs the cast alone (BucketStartUtc is already floored to the hour); the
        // daily case's DATEADD(..., 0) third-argument literal already yields a plain datetime,
        // same as the raw path's equivalent expression.
        var bucketExpr = hourly ? "CAST(r.BucketStartUtc AS datetime2)" : "DATEADD(DAY, DATEDIFF(DAY, 0, r.BucketStartUtc), 0)";

        return $@"
SELECT
    {bucketExpr} AS BucketStart,
    SUM(r.EventCount) AS Total,
    SUM(CASE WHEN ad.Name = N'Allow' THEN r.EventCount ELSE 0 END) AS Allowed,
    SUM(CASE WHEN ad.Name = N'Block' THEN r.EventCount ELSE 0 END) AS Blocked,
    SUM(CASE WHEN ad.Name IN (N'AuditOnly', N'Audit') THEN r.EventCount ELSE 0 END) AS AuditOnly,
    SUM(CASE WHEN ad.Name = N'Error' THEN r.EventCount ELSE 0 END) AS Errors
FROM dbo.DashboardHourlyRollups r
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = r.DecisionId
WHERE r.OrganizationId = @OrganizationId
  AND r.TimeBasis = 0
  AND r.BucketStartUtc >= @FromUtc AND r.BucketStartUtc < @ToUtc
GROUP BY {bucketExpr};";
    }

    // --- SQL (against the rollup tables, not AuditEvents) -------------------------------------

    private const string TotalsSql = @"
SELECT
    SUM(r.EventCount) AS TotalEvents,
    SUM(CASE WHEN ad.Name = N'Allow' THEN r.EventCount ELSE 0 END) AS AllowedEvents,
    SUM(CASE WHEN ad.Name = N'Block' THEN r.EventCount ELSE 0 END) AS BlockedEvents,
    SUM(CASE WHEN ad.Name IN (N'AuditOnly', N'Audit') THEN r.EventCount ELSE 0 END) AS AuditOnlyEvents,
    SUM(CASE WHEN ad.Name = N'Error' THEN r.EventCount ELSE 0 END) AS ErrorEvents
FROM dbo.DashboardHourlyRollups r
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = r.DecisionId
WHERE r.OrganizationId = @OrganizationId
  AND r.TimeBasis = 0
  AND r.BucketStartUtc >= @FromUtc AND r.BucketStartUtc < @ToUtc;
";

    private const string ActionBreakdownTop8Sql = @"
SELECT TOP (8)
    r.ActionKey,
    SUM(r.EventCount) AS Total,
    SUM(CASE WHEN ad.Name = N'Allow' THEN r.EventCount ELSE 0 END) AS Allowed,
    SUM(CASE WHEN ad.Name = N'Block' THEN r.EventCount ELSE 0 END) AS Blocked,
    SUM(CASE WHEN ad.Name = N'Error' THEN r.EventCount ELSE 0 END) AS Errors,
    MAX(r.LastReceivedAtUtc) AS LastReceivedAtUtc
FROM dbo.DashboardHourlyRollups r
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = r.DecisionId
WHERE r.OrganizationId = @OrganizationId
  AND r.TimeBasis = 0
  AND r.BucketStartUtc >= @FromUtc AND r.BucketStartUtc < @ToUtc
GROUP BY r.ActionKey
ORDER BY SUM(r.EventCount) DESC, r.ActionKey ASC;
";

    private const string ReasonBreakdownTop8Sql = @"
SELECT TOP (8)
    COALESCE(arc.Code, N'Unknown') AS ReasonCode,
    SUM(r.EventCount) AS Total
FROM dbo.DashboardHourlyRollups r
LEFT JOIN dbo.AuditReasonCodes arc ON arc.Id = r.ReasonCodeId
WHERE r.OrganizationId = @OrganizationId
  AND r.TimeBasis = 0
  AND r.BucketStartUtc >= @FromUtc AND r.BucketStartUtc < @ToUtc
GROUP BY COALESCE(arc.Code, N'Unknown')
ORDER BY SUM(r.EventCount) DESC;
";

    private const string ChannelBreakdownSql = @"
SELECT pac.Name AS Category, SUM(r.EventCount) AS Blocked
FROM dbo.DashboardHourlyRollups r
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = r.DecisionId
LEFT JOIN dbo.PermissionActions pa ON pa.[Key] = r.ActionKey
LEFT JOIN dbo.PermissionActionCategories pac ON pac.Id = pa.CategoryId
WHERE r.OrganizationId = @OrganizationId
  AND r.TimeBasis = 0
  AND r.BucketStartUtc >= @FromUtc AND r.BucketStartUtc < @ToUtc
  AND ad.Name = N'Block'
  AND pac.Name IS NOT NULL
  AND pac.Name <> N'System'
GROUP BY pac.Name
ORDER BY SUM(r.EventCount) DESC;
";

    // Same shape/semantics as SqlDlpDashboardQueryService.RiskBlockCountSql - an employee with zero
    // rollup rows in the window still appears (LEFT JOIN from Employees), with BlockCount 0.
    private const string RiskBlockCountSql = @"
SELECT e.Id AS EmployeeId, e.DisplayName, d.Name AS DepartmentName,
       SUM(CASE WHEN ad.Name = N'Block' THEN r.EventCount ELSE 0 END) AS BlockCount
FROM dbo.Employees e
LEFT JOIN dbo.Departments d ON d.Id = e.DepartmentId
LEFT JOIN dbo.DashboardEmployeeDailyRollups r ON r.EmployeeId = e.Id
    AND r.BucketDateUtc >= @RiskFromUtc AND r.BucketDateUtc < @RiskToUtc
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = r.DecisionId
WHERE e.OrganizationId = @OrganizationId
GROUP BY e.Id, e.DisplayName, d.Name;
";

    private const string RiskFactorSql = @"
SELECT r.EmployeeId, pac.Name AS Category, SUM(r.EventCount) AS Cnt
FROM dbo.DashboardEmployeeDailyRollups r
INNER JOIN dbo.Employees e ON e.Id = r.EmployeeId AND e.OrganizationId = @OrganizationId
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = r.DecisionId
LEFT JOIN dbo.PermissionActions pa ON pa.[Key] = r.ActionKey
LEFT JOIN dbo.PermissionActionCategories pac ON pac.Id = pa.CategoryId
WHERE r.BucketDateUtc >= @RiskFromUtc AND r.BucketDateUtc < @RiskToUtc
  AND ad.Name = N'Block'
  AND pac.Name IS NOT NULL
  AND pac.Name <> N'System'
GROUP BY r.EmployeeId, pac.Name;
";

    // --- Readers (own lean-totals reader; everything else reuses SqlDlpDashboardQueryService's
    // internal static ones - same column names, same shapes) ---------------------------------

    private static async Task<DlpKpiTotalsDto> ReadLeanTotalsAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new DlpKpiTotalsDto(0, 0, 0, 0, 0);
        }

        return new DlpKpiTotalsDto(
            SqlDlpDashboardQueryService.GetLong(reader, "TotalEvents"),
            SqlDlpDashboardQueryService.GetLong(reader, "AllowedEvents"),
            SqlDlpDashboardQueryService.GetLong(reader, "BlockedEvents"),
            SqlDlpDashboardQueryService.GetLong(reader, "AuditOnlyEvents"),
            SqlDlpDashboardQueryService.GetLong(reader, "ErrorEvents"));
    }

    private static async Task<DlpKpiTotalsDto> GetTotalsAsync(
        SqlConnection connection, Guid organizationId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, TotalsSql, organizationId, from, to);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await ReadLeanTotalsAsync(reader, cancellationToken);
    }

    private static async Task<IReadOnlyList<DlpActionBreakdownItemDto>> ReadActionBreakdownAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new List<DlpActionBreakdownItemDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DlpActionBreakdownItemDto(
                SqlDlpDashboardQueryService.GetString(reader, "ActionKey"),
                SqlDlpDashboardQueryService.GetLong(reader, "Total"),
                SqlDlpDashboardQueryService.GetLong(reader, "Allowed"),
                SqlDlpDashboardQueryService.GetLong(reader, "Blocked"),
                SqlDlpDashboardQueryService.GetLong(reader, "Errors"),
                SqlDlpDashboardQueryService.GetNullableDateTimeOffset(reader, "LastReceivedAtUtc")));
        }
        return result;
    }

    private static async Task<IReadOnlyList<DlpReasonBreakdownItemDto>> ReadReasonBreakdownAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new List<DlpReasonBreakdownItemDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DlpReasonBreakdownItemDto(
                SqlDlpDashboardQueryService.GetString(reader, "ReasonCode"),
                SqlDlpDashboardQueryService.GetLong(reader, "Total")));
        }
        return result;
    }

    private static async Task<IReadOnlyList<DlpChannelBreakdownItemDto>> ReadChannelBreakdownAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new List<DlpChannelBreakdownItemDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DlpChannelBreakdownItemDto(
                SqlDlpDashboardQueryService.GetString(reader, "Category"),
                SqlDlpDashboardQueryService.GetLong(reader, "Blocked")));
        }
        return result;
    }

    private static async Task<List<(Guid EmployeeId, string DisplayName, string? DepartmentName, long BlockCount)>> ReadRiskBlockCountsAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        var employeeRows = new List<(Guid EmployeeId, string DisplayName, string? DepartmentName, long BlockCount)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            employeeRows.Add((
                reader.GetGuid(reader.GetOrdinal("EmployeeId")),
                SqlDlpDashboardQueryService.GetString(reader, "DisplayName"),
                SqlDlpDashboardQueryService.GetNullableString(reader, "DepartmentName"),
                SqlDlpDashboardQueryService.GetLong(reader, "BlockCount")));
        }
        return employeeRows;
    }

    private static async Task<Dictionary<Guid, List<DlpRiskFactorDto>>> ReadRiskFactorsAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var factorsByEmployee = new Dictionary<Guid, List<DlpRiskFactorDto>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var employeeId = reader.GetGuid(reader.GetOrdinal("EmployeeId"));
            var factor = new DlpRiskFactorDto(SqlDlpDashboardQueryService.GetString(reader, "Category"), SqlDlpDashboardQueryService.GetLong(reader, "Cnt"));

            if (!factorsByEmployee.TryGetValue(employeeId, out var list))
            {
                list = new List<DlpRiskFactorDto>();
                factorsByEmployee[employeeId] = list;
            }
            list.Add(factor);
        }
        return factorsByEmployee;
    }

    private static SqlCommand CreateCommand(SqlConnection connection, string sql, Guid organizationId, DateTimeOffset from, DateTimeOffset to)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        command.Parameters.Add(new SqlParameter("@OrganizationId", SqlDbType.UniqueIdentifier) { Value = organizationId });
        command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTimeOffset) { Value = from });
        command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTimeOffset) { Value = to });
        return command;
    }
}
