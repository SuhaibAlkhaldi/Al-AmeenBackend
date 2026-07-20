using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class ObservedFile
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid? DeviceId { get; set; }

    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [StringLength(20)]
    public string? FileExtension { get; set; }

    [StringLength(150)]
    public string? MimeType { get; set; }

    [StringLength(1000)]
    public string? FilePath { get; set; }

    public long? FileSizeBytes { get; set; }

    [StringLength(128)]
    public string? Sha256Hash { get; set; }

    public bool IsSensitive { get; set; }

    public DateTimeOffset FirstSeenAtUtc { get; set; }

    public DateTimeOffset? LastSeenAtUtc { get; set; }

    [InverseProperty("ObservedFile")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [ForeignKey("DeviceId")]
    [InverseProperty("ObservedFiles")]
    public virtual Device? Device { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("ObservedFiles")]
    public virtual Organization Organization { get; set; } = null!;
}
