using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Table("PermissionRequestHistory")]
public partial class PermissionRequestHistory
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid PermissionRequestId { get; set; }

    public Guid ChangedByUserId { get; set; }

    public int? FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = null!;

    [StringLength(2000)]
    public string? Description { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset ChangedAtUtc { get; set; }

    [ForeignKey("ChangedByUserId")]
    [InverseProperty("PermissionRequestHistories")]
    public virtual User ChangedByUser { get; set; } = null!;

    [ForeignKey("FromStatusId")]
    [InverseProperty("PermissionRequestHistoryFromStatuses")]
    public virtual PermissionRequestStatus? FromStatus { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("PermissionRequestHistories")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("PermissionRequestId")]
    [InverseProperty("PermissionRequestHistories")]
    public virtual PermissionRequest PermissionRequest { get; set; } = null!;

    [ForeignKey("ToStatusId")]
    [InverseProperty("PermissionRequestHistoryToStatuses")]
    public virtual PermissionRequestStatus ToStatus { get; set; } = null!;
}
