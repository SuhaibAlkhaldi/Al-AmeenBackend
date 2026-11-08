using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Per-organization tier->action mapping for the Email DLP policy page (Phase 3, page B). Composite
// key (OrganizationId, ClassificationTier) - one row per tier the admin has explicitly configured;
// a missing row falls back to EmailPolicyEngine's own hardcoded default for that tier (see the
// Email DLP plan's spec items 3-4), so this table only needs to hold overrides, not every tier.
[PrimaryKey("OrganizationId", "ClassificationTier")]
public partial class EmailPolicySetting
{
    public Guid OrganizationId { get; set; }

    [StringLength(20)]
    public string ClassificationTier { get; set; } = null!;

    // One of Allow/Block/Quarantine/RequireApproval - matches Ameen.EmailDLP.Domain.PolicyAction's
    // names exactly (stored as a string rather than referencing that enum directly, since this
    // model lives in DLPManagementSystem and shouldn't take a hard dependency on the Email DLP
    // solution's own enum type).
    [StringLength(50)]
    public string Action { get; set; } = null!;

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
