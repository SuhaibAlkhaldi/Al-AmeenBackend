using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Name", Name = "UQ_PermissionGrantTypes_Name", IsUnique = true)]
public partial class PermissionGrantType
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [InverseProperty("GrantType")]
    public virtual ICollection<PermissionGrant> PermissionGrants { get; set; } = new List<PermissionGrant>();

    [InverseProperty("RequestedGrantType")]
    public virtual ICollection<PermissionRequest> PermissionRequests { get; set; } = new List<PermissionRequest>();
}
