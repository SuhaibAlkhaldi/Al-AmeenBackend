using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Name", Name = "UQ_PermissionSubjectTypes_Name", IsUnique = true)]
public partial class PermissionSubjectType
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [InverseProperty("SubjectType")]
    public virtual ICollection<PermissionGrant> PermissionGrants { get; set; } = new List<PermissionGrant>();

    [InverseProperty("SubjectType")]
    public virtual ICollection<PermissionRequest> PermissionRequests { get; set; } = new List<PermissionRequest>();

    [InverseProperty("ApprovedForSubjectType")]
    public virtual ICollection<UsbDeviceApproval> UsbDeviceApprovals { get; set; } = new List<UsbDeviceApproval>();
}
