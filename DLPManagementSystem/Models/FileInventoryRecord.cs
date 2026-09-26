using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Current-state inventory of files inside each device's watched folders (File Inventory Report
// feature). One row per (DeviceId, FilePath) currently believed to exist on disk - NOT an
// append-only event log like AuditEvent. FileId is an independent database identity; it does not
// carry any meaning about a file's identity persisting across a rename/move (see the unique index
// below and AgentFileInventoryService's rename handling: a rename always soft-deletes the old row
// and inserts a brand-new row/FileId, by deliberate 2026 product decision - tracking "this is still
// the same file" across a rename is out of scope for v1 and left to a possible future Provenance
// lineage feature). FileHash is a content fingerprint used only to look up a known classification -
// it is not part of this table's identity either.
[Index("DeviceId", "IsDeleted", Name = "IX_FileInventoryRecords_Device_Active")]
[Index("OrganizationId", "ClassificationTier", Name = "IX_FileInventoryRecords_Org_Classification")]
public partial class FileInventoryRecord
{
    [Key]
    public Guid FileId { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid DeviceId { get; set; }

    public Guid? EmployeeId { get; set; }

    [StringLength(1000)]
    public string FilePath { get; set; } = null!;

    [StringLength(255)]
    public string FileName { get; set; } = null!;

    // Content fingerprint only - see class comment. Never used to look up this row; only to carry
    // a known classification forward for files whose content hasn't changed.
    [StringLength(128)]
    public string? FileHash { get; set; }

    public int? FileTypeId { get; set; }

    // Raw extension fallback for files whose extension isn't in the FileTypes lookup table yet -
    // same "FK with raw fallback" idiom AuditEvent uses for ReasonCodeId/raw reason code.
    [StringLength(20)]
    public string? Extension { get; set; }

    public long SizeBytes { get; set; }

    [StringLength(20)]
    public string ClassificationTier { get; set; } = null!;

    [StringLength(20)]
    public string Provenance { get; set; } = null!;

    public bool IsProtected { get; set; }

    public DateTimeOffset LastScanAtUtc { get; set; }

    public DateTimeOffset FirstSeenAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    // Soft delete - see UX_FileInventory_Active_DeviceId_FilePath in DLPSystemContext.OnModelCreating.
    // A background job (FileInventoryCleanupWorker) hard-deletes rows once DeletedAtUtc is more than
    // FileInventoryCleanupOptions.RetentionDays (90 by default) in the past.
    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("FileInventoryRecords")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("FileInventoryRecords")]
    public virtual Employee? Employee { get; set; }

    [ForeignKey("FileTypeId")]
    [InverseProperty("FileInventoryRecords")]
    public virtual FileType? FileType { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("FileInventoryRecords")]
    public virtual Organization Organization { get; set; } = null!;
}
