using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[PrimaryKey("DeviceGroupId", "DeviceId")]
public partial class DeviceGroupMember
{
    [Key]
    public Guid DeviceGroupId { get; set; }

    [Key]
    public Guid DeviceId { get; set; }

    public DateTimeOffset AddedAtUtc { get; set; }

    public Guid? AddedByUserId { get; set; }

    public DateTimeOffset? RemovedAtUtc { get; set; }

    [ForeignKey("AddedByUserId")]
    [InverseProperty("DeviceGroupMembers")]
    public virtual User? AddedByUser { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("DeviceGroupMembers")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("DeviceGroupId")]
    [InverseProperty("DeviceGroupMembers")]
    public virtual DeviceGroup DeviceGroup { get; set; } = null!;
}
