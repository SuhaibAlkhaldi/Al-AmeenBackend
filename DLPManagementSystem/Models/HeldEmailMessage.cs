using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// A message the Email DLP Worker held instead of relaying (overall tier above Public - see the
// Email DLP plan's spec items 3-4). RawMimeContent keeps the exact bytes the gateway received so
// Approve can relay the original message unchanged, the same way an Allow decision does.
//
// OrganizationId/ReviewedByUserId are plain scalar columns with no FK navigation/constraint
// configured - deliberately, to avoid touching Organization.cs/User.cs's own InverseProperty
// collections for this first version. Revisit if referential integrity at the DB level becomes
// necessary later.
[Index("OrganizationId", "StatusId", "CreatedAtUtc", Name = "IX_HeldEmailMessages_Organization_Status")]
public partial class HeldEmailMessage
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    // Matches the id Ameen.EmailDLP.Worker generated for this message (GatewayMessageStore) - lets
    // the EmailEvent row for the same message be found without a DB-level FK between the two
    // separate solutions.
    [StringLength(100)]
    public string MessageId { get; set; } = null!;

    [StringLength(320)]
    public string FromAddress { get; set; } = null!;

    // Comma-joined To+Cc+Bcc addresses - simple scalar storage rather than a child table, since
    // this is a read-mostly review queue, not a relational query target.
    [StringLength(2000)]
    public string ToAddresses { get; set; } = null!;

    [StringLength(500)]
    public string Subject { get; set; } = null!;

    [StringLength(20)]
    public string ClassificationTier { get; set; } = null!;

    [StringLength(2000)]
    public string ReasonCode { get; set; } = null!;

    // The PolicyAction the policy engine decided (Block/Quarantine/RequireApproval) - distinct from
    // StatusId, which tracks the admin's review outcome (Held/Approved/Rejected).
    [StringLength(50)]
    public string Action { get; set; } = null!;

    public byte[] RawMimeContent { get; set; } = null!;

    public int StatusId { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAtUtc { get; set; }

    [StringLength(2000)]
    public string? ReviewNotes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    [ForeignKey("StatusId")]
    [InverseProperty("HeldEmailMessages")]
    public virtual HeldEmailMessageStatus Status { get; set; } = null!;
}
