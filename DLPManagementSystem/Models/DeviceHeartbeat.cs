using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("DeviceId", "OccurredAtUtc", Name = "IX_DeviceHeartbeats_Device_Occurred", IsDescending = new[] { false, true })]
public partial class DeviceHeartbeat
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid DeviceId { get; set; }

    [StringLength(50)]
    public string? AgentVersion { get; set; }

    public long PolicyVersion { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(250)]
    public string? LoggedInUserSid { get; set; }

    [StringLength(150)]
    public string? LoggedInUsername { get; set; }

    public string? StatusJson { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("DeviceHeartbeats")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("DeviceHeartbeats")]
    public virtual Organization Organization { get; set; } = null!;
}
