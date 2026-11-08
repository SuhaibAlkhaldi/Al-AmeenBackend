using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// The full audit trail the Email DLP plan's spec items 3-5 require: one row per processed email,
// whatever its outcome (auto-allowed, approved, rejected, or sent via an active bypass grant) - so
// an admin can review history even for mail nobody ever had to act on. Deliberately separate from
// the agent's own AuditEvent table (different domain - devices/files vs. email messages).
[Index("OrganizationId", "OccurredAtUtc", Name = "IX_EmailEvents_Organization_OccurredAt", IsDescending = new[] { false, true })]
public partial class EmailEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(100)]
    public string MessageId { get; set; } = null!;

    [StringLength(320)]
    public string FromAddress { get; set; } = null!;

    [StringLength(2000)]
    public string ToAddresses { get; set; } = null!;

    [StringLength(500)]
    public string Subject { get; set; } = null!;

    [StringLength(20)]
    public string ClassificationTier { get; set; } = null!;

    // The PolicyAction the engine decided (Allow/Block/Quarantine/RequireApproval).
    [StringLength(50)]
    public string Action { get; set; } = null!;

    // What actually happened to the message: AutoAllowed, Approved, Rejected, or BypassFired.
    [StringLength(50)]
    public string Outcome { get; set; } = null!;

    [StringLength(2000)]
    public string ReasonCode { get; set; } = null!;

    // Set only when this event is tied to a row in HeldEmailMessages (Approved/Rejected) - null for
    // AutoAllowed/BypassFired, which never created one. No FK constraint configured, for the same
    // reason HeldEmailMessage's own OrganizationId/ReviewedByUserId aren't - see that class's comment.
    public Guid? HeldEmailMessageId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
