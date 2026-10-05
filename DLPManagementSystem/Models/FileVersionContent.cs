using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// The extracted text of one FileVersionHistory row, kept for the File Tracking diff viewer (Phase 6). Only the
// sensitive tiers carry a row here (see the agent's FileVersionTextCapture), and only content-changing versions.
//
// Security: EncryptedText is the DataProtection payload (purpose "CompanyDlp.FileVersionContent"), never plaintext.
// The key ring is persisted to the server's file system and protected by Windows DPAPI by default, so a database
// copy on its own cannot be read. The text is never written to logs. Rows are purged once they are older than
// FileInventoryCleanup:RetentionDays (see FileInventoryCleanupWorker), while the history rows themselves are kept.
//
// The primary key is the history row's own Id, so each version has at most one text and the text is removed
// by cascade if its history row is ever deleted.
[Index("CapturedAtUtc", Name = "IX_FileVersionContents_CapturedAtUtc")]
[Index("OrganizationId", "FileId", Name = "IX_FileVersionContents_Org_File")]
public partial class FileVersionContent
{
    [Key]
    public Guid FileVersionHistoryId { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid FileId { get; set; }

    public DateTimeOffset CapturedAtUtc { get; set; }

    // Length of the plaintext in characters. Stored so a diff can report its size without decrypting first.
    public int CharCount { get; set; }

    public string EncryptedText { get; set; } = null!;

    [ForeignKey("FileVersionHistoryId")]
    public virtual FileVersionHistory FileVersionHistory { get; set; } = null!;
}
