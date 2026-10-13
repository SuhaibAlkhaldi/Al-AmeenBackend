using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Which raw AuditEvents timestamp column a row was bucketed from - kpis/breakdown read Received
// rows, weekly-trend reads Occurred rows. See DASHBOARD_ENDPOINTS.md's "Rollup" section for why the
// two can never be unified into one column without changing either endpoint's visible numbers.
public enum DashboardRollupTimeBasis : byte
{
    Received = 0,
    Occurred = 1
}

// dashboard-rollup: one row per (org, time-basis, hour, decision, action, reason) combination -
// the grain is bounded by how many distinct ActionKey/ReasonCode/Decision values exist, not by how
// many raw AuditEvents rows exist, so this stays small (tens-hundreds of rows per org per hour)
// even when AuditEvents itself holds tens of millions. Written only by
// DashboardRollupRefreshService (delete-then-insert per (org, basis, bucket range), inside a short
// transaction - see its own comments for why that's safe without RCSI/snapshot isolation).
// EventCount is a plain SUM; LastReceivedAtUtc/LastOccurredAtUtc feed the "last seen" columns the
// raw action-breakdown query also reports, so the rollup path doesn't have to drop that field.
[Index("OrganizationId", "TimeBasis", "BucketStartUtc", Name = "IX_DashboardHourlyRollup_Org_Basis_Bucket")]
public partial class DashboardHourlyRollup
{
    [Key]
    public long Id { get; set; }

    public Guid OrganizationId { get; set; }

    public DashboardRollupTimeBasis TimeBasis { get; set; }

    // Always floored to the hour (minute/second/fraction always zero) - the rollup's own grain.
    public DateTimeOffset BucketStartUtc { get; set; }

    public int DecisionId { get; set; }

    [StringLength(100)]
    public string ActionKey { get; set; } = null!;

    // Null means "no reason code on these events" - same optionality as AuditEvent.ReasonCodeId.
    public int? ReasonCodeId { get; set; }

    public long EventCount { get; set; }

    public DateTimeOffset LastReceivedAtUtc { get; set; }

    public DateTimeOffset LastOccurredAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
