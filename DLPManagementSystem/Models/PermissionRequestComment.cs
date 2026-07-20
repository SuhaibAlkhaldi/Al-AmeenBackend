using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class PermissionRequestComment
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid PermissionRequestId { get; set; }

    public Guid CreatedByUserId { get; set; }

    [StringLength(2000)]
    public string CommentText { get; set; } = null!;

    public bool IsInternal { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("PermissionRequestComments")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("PermissionRequestComments")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("PermissionRequestId")]
    [InverseProperty("PermissionRequestComments")]
    public virtual PermissionRequest PermissionRequest { get; set; } = null!;
}
