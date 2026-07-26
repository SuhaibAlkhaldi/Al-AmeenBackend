using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class PermissionRequestAttachment
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid PermissionRequestId { get; set; }

    public Guid UploadedByUserId { get; set; }

    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [StringLength(255)]
    public string OriginalFileName { get; set; } = null!;

    [StringLength(150)]
    public string? ContentType { get; set; }

    public long SizeInBytes { get; set; }

    [StringLength(1000)]
    public string StoragePath { get; set; } = null!;

    [StringLength(128)]
    public string? Sha256Hash { get; set; }

    // Populated for rows created from an agent-observed blocked attempt (the exact-file request
    // path): the AI classifier's tier for this file and its reasoning, both looked up server-side
    // from the source AuditEvent - never trusted from client input. Null for any attachment row
    // that isn't tied to a classified file.
    [StringLength(20)]
    public string? Classification { get; set; }

    [StringLength(500)]
    public string? ClassificationReasonCode { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("PermissionRequestAttachments")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("PermissionRequestId")]
    [InverseProperty("PermissionRequestAttachments")]
    public virtual PermissionRequest PermissionRequest { get; set; } = null!;

    [ForeignKey("UploadedByUserId")]
    [InverseProperty("PermissionRequestAttachments")]
    public virtual User UploadedByUser { get; set; } = null!;
}
