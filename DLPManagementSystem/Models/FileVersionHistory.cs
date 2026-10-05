using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Append-only history of every real change to a FileInventoryRecord (Phase 1 of the "File Tracking"
// plan - see the approved plan file for the full design). Unlike FileInventoryRecord (current-state,
// one active row per device+path), this table never updates or deletes existing rows - every genuine
// transition (including a file's first-ever appearance, a "Genesis" row with all Old* fields null)
// gets its own row, forever. Written by AgentFileInventoryService.UpsertActiveRecord in the same
// SaveChanges call (hence the same DB transaction) as the FileInventoryRecords upsert it accompanies,
// and only when the incoming values genuinely differ from what's already in the database (DB is the
// source of truth for Old* - never trust an "old" value asserted by the agent) - a retried/duplicate
// batch delivery that reproduces an already-applied state writes no new row.
//
// FileId links to FileInventoryRecord.FileId, not to FileHash - the hash itself is one of the things
// that can change between rows for the same FileId. No VersionNumber is stored; display ordering is
// derived at query time via ROW_NUMBER() OVER (PARTITION BY FileId ORDER BY ObservedAtUtc), avoiding a
// redundant, potentially-racy stored counter.
[Index("FileId", "ObservedAtUtc", Name = "IX_FileVersionHistory_File_Observed")]
[Index("NewFileHash", "ObservedAtUtc", Name = "IX_FileVersionHistory_NewHash_Observed")]
public partial class FileVersionHistory
{
    [Key]
    public Guid Id { get; set; }

    public Guid FileId { get; set; }

    public Guid OrganizationId { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }

    [StringLength(128)]
    public string? OldFileHash { get; set; }

    [StringLength(128)]
    public string NewFileHash { get; set; } = null!;

    [StringLength(20)]
    public string? OldClassificationTier { get; set; }

    [StringLength(20)]
    public string NewClassificationTier { get; set; } = null!;

    public bool? OldIsProtected { get; set; }

    public bool NewIsProtected { get; set; }

    public bool ContentChanged { get; set; }

    public bool ClassificationChanged { get; set; }

    public bool ProtectionChanged { get; set; }

    // "Change" for an ordinary content/classification/protection transition (the default for every
    // existing row), or "FullCopy" for a row recording that this file's content was found identical to
    // another file's (SourceFileId) at this moment. A FullCopy row is an event, not a state transition,
    // so it is kept even after this file's content changes again.
    [StringLength(32)]
    public string EventType { get; set; } = "Change";

    public Guid? SourceFileId { get; set; }

    // The content fingerprint (see the agent's ContentFingerprinter) of the content this row describes.
    // Phase 5 matches copies on it because a file's byte hash changes per device (each device stamps its
    // own header). Null for rows written before this column existed and for formats the agent cannot read.
    [StringLength(64)]
    public string? NewContentFingerprint { get; set; }

    public Guid DeviceId { get; set; }

    public Guid? EmployeeId { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("FileVersionHistories")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("FileVersionHistories")]
    public virtual Employee? Employee { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("FileVersionHistories")]
    public virtual Organization Organization { get; set; } = null!;
}
