using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Name", Name = "UQ_DeviceStatuses_Name", IsUnique = true)]
public partial class DeviceStatus
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [InverseProperty("Status")]
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
}
