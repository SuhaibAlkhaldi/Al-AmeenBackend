using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// dashboard-rollup: per-employee, per-day counts feeding /risk's score (all decisions' Block count
// per employee) and "top factors" (per-category Block count, System category excluded at read
// time - see RiskFactorSql's own exclusion, replicated by the read side here rather than baked
// into this table, so a single ActionKey dimension serves both). Always bucketed from
// OccurredAtUtc, floored to the day (UTC midnight) - risk's own window. Grain is bounded by
// (active employees x distinct ActionKeys), not by raw event count.
[Index("OrganizationId", "BucketDateUtc", Name = "IX_DashboardEmployeeDailyRollup_Org_Bucket")]
public partial class DashboardEmployeeDailyRollup
{
    [Key]
    public long Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid EmployeeId { get; set; }

    // Always floored to the day (00:00:00 UTC) - the rollup's own grain.
    public DateTimeOffset BucketDateUtc { get; set; }

    [StringLength(100)]
    public string ActionKey { get; set; } = null!;

    public int DecisionId { get; set; }

    public long EventCount { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
