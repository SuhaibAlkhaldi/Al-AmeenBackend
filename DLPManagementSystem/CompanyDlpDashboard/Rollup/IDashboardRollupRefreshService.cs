namespace DLPManagementSystem.CompanyDlpDashboard.Rollup;

public sealed record DashboardRollupRefreshResult(
    bool LockAcquired,
    int OrganizationsProcessed,
    long HourlyRowsWritten,
    long EmployeeDailyRowsWritten,
    TimeSpan Elapsed);

public sealed record DashboardRollupBackfillChunkResult(
    Guid OrganizationId,
    string RollupKind,
    bool MoreRemaining,
    DateTimeOffset NewWatermarkUtc,
    long RowsWritten);

public interface IDashboardRollupRefreshService
{
    /// <summary>One recompute cycle: re-aggregates the trailing LateEventLookbackHours (Hourly
    /// Received+Occurred) and EmployeeRollupRecomputeDays (EmployeeDaily) window for every
    /// organization, inside a single distributed lock so at most one instance ever does this at
    /// once. Returns immediately with LockAcquired=false (does no work) if another instance
    /// already holds the lock - this is the normal, expected outcome on most ticks when more than
    /// one app instance is running, not an error.</summary>
    Task<DashboardRollupRefreshResult> RefreshRecentWindowAsync(CancellationToken cancellationToken);

    /// <summary>Extends one organization's watermark for one rollup kind one chunk
    /// (BackfillChunkDays) further into the past, aggregating that chunk from raw AuditEvents and
    /// writing it in. Safe to call repeatedly/resume after an interruption - it always continues
    /// from the organization's current watermark, never from the beginning. MoreRemaining=false
    /// once the chunk reaches (or passes) the organization's earliest AuditEvents row for that
    /// basis - further calls are then no-ops.</summary>
    Task<DashboardRollupBackfillChunkResult> BackfillOneChunkAsync(
        Guid organizationId,
        DashboardRollupKindSelector kind,
        CancellationToken cancellationToken);
}

/// <summary>Which of the three rollup kinds a backfill call targets - mirrors
/// DashboardRollupKind, kept as its own small enum here (rather than reusing the model one
/// directly) so this service's public surface doesn't require callers to reference
/// DLPManagementSystem.Models for a simple selector.</summary>
public enum DashboardRollupKindSelector
{
    HourlyReceived,
    HourlyOccurred,
    EmployeeDaily
}
