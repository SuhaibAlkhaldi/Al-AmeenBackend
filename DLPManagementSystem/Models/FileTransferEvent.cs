using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// One observed send of a sensitive file to one recipient (Phase 7, see the frozen evidence spec). Append-only: a row is
// written once and never edited, so the rules can change later (CorrelationRuleVersion says which rules produced it)
// without rewriting history. One Gmail send to three recipients produces three rows sharing SendActionId.
//
// Stored: the recipient address, normalized. Never stored: message body, subject, contact lists, display names,
// group members, page URLs, cookies. Retention is handled by FileInventoryCleanupWorker (ReceivedAtUtc).
[Index("OrganizationId", "ObservedAtUtc", Name = "IX_FileTransferEvents_Org_Observed")]
[Index("OrganizationId", "FileId", Name = "IX_FileTransferEvents_Org_File")]
[Index("OrganizationId", "ReceivedAtUtc", Name = "IX_FileTransferEvents_Org_Received")]
[Index("OrganizationId", "SendActionId", "RecipientOrdinal", Name = "UX_FileTransferEvents_Send_Recipient", IsUnique = true)]
public partial class FileTransferEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid SendActionId { get; set; }

    public int RecipientOrdinal { get; set; }

    public Guid SourceDeviceId { get; set; }

    public Guid? SourceEmployeeId { get; set; }

    [StringLength(20)]
    public string Channel { get; set; } = null!;

    [StringLength(4)]
    public string? RecipientRole { get; set; }

    [StringLength(20)]
    public string RecipientKind { get; set; } = null!;

    [StringLength(320)]
    public string RecipientValue { get; set; } = null!;

    [StringLength(40)]
    public string RecipientEvidenceType { get; set; } = null!;

    // Set when the file matched an inventory record. Null for an ambiguous match, where the file is not known for certain.
    public Guid? FileId { get; set; }

    [StringLength(64)]
    public string? ContentFingerprint { get; set; }

    [StringLength(64)]
    public string? FileHashBytes { get; set; }

    [StringLength(260)]
    public string FileName { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    [StringLength(20)]
    public string ClassificationTierAtEvent { get; set; } = null!;

    [StringLength(32)]
    public string FileEvidenceType { get; set; } = null!;

    [StringLength(8)]
    public string FileEvidenceLevel { get; set; } = null!;

    [StringLength(8)]
    public string RecipientEvidenceLevel { get; set; } = null!;

    [StringLength(8)]
    public string OverallEvidenceLevel { get; set; } = null!;

    public DateTimeOffset ObservedAtUtc { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    [StringLength(32)]
    public string? ExtensionVersion { get; set; }

    public int CorrelationRuleVersion { get; set; }

    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("SourceDeviceId")]
    public virtual Device SourceDevice { get; set; } = null!;
}
