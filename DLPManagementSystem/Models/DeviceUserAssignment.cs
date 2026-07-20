using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class DeviceUserAssignment
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid DeviceId { get; set; }

    public Guid EmployeeId { get; set; }

    [StringLength(250)]
    public string UserSid { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public DateTimeOffset AssignedAtUtc { get; set; }

    public DateTimeOffset? UnassignedAtUtc { get; set; }

    public Guid? AssignedByUserId { get; set; }

    [ForeignKey("AssignedByUserId")]
    [InverseProperty("DeviceUserAssignments")]
    public virtual User? AssignedByUser { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("DeviceUserAssignments")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("DeviceUserAssignments")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("DeviceUserAssignments")]
    public virtual Organization Organization { get; set; } = null!;
}
