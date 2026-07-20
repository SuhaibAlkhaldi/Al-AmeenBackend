using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Name", Name = "UQ_PermissionRequestStatuses_Name", IsUnique = true)]
public partial class PermissionRequestStatus
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [InverseProperty("FromStatus")]
    public virtual ICollection<PermissionRequestHistory> PermissionRequestHistoryFromStatuses { get; set; } = new List<PermissionRequestHistory>();

    [InverseProperty("ToStatus")]
    public virtual ICollection<PermissionRequestHistory> PermissionRequestHistoryToStatuses { get; set; } = new List<PermissionRequestHistory>();

    [InverseProperty("Status")]
    public virtual ICollection<PermissionRequest> PermissionRequests { get; set; } = new List<PermissionRequest>();
}
