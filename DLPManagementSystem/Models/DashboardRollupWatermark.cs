using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Which rolling window DashboardRollupRefreshService maintains for one organization. The two
// Hourly kinds share DashboardHourlyRollup's own TimeBasis discriminator; EmployeeDaily is its own
// table/grain entirely (see DashboardEmployeeDailyRollup).
public enum DashboardRollupKind
{
    HourlyReceived,
    HourlyOccurred,
    EmployeeDaily
}

// dashboard-rollup: "how far has this organization's rollup actually been computed, and by what
// (backfill vs. the regular refresh cycle)" - read by:
//   - the dashboard read path, to know how fresh/stale the numbers it's serving are (not currently
//     surfaced to the frontend, but available for an ops/health check later);
//   - AuditRetentionWorker's new guard, which must never purge a raw AuditEvents row older than
//     this for a given org+basis - a row the rollup hasn't counted yet must not disappear first;
//   - the manual backfill command, to resume a previous (possibly interrupted) run from where it
//     left off rather than restarting from the very beginning.
// LastComputedThroughUtc is EXCLUSIVE - "every row with the relevant timestamp < this value has
// been folded into the rollup"; nothing lets this value move backwards (see
// DashboardRollupRefreshService.AdvanceWatermarkAsync).
[PrimaryKey(nameof(OrganizationId), nameof(RollupKind))]
public sealed class DashboardRollupWatermark
{
    public Guid OrganizationId { get; set; }

    [MaxLength(20)]
    public string RollupKind { get; set; } = null!;

    public DateTimeOffset LastComputedThroughUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    // Only meaningful on the EmployeeDaily row: when the daily wide-sweep safety net (see
    // DashboardRollupOptions.EmployeeRollupWideSweepEnabled) last ran for this organization - null
    // if it has never run (or the sweep is disabled). Read-then-checked under the same
    // sp_getapplock as the sweep itself, so two instances can never both run it the same day.
    public DateTimeOffset? LastWideSweepUtc { get; set; }
}
