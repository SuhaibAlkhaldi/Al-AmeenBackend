using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("LastSeenAtUtc", Name = "IX_Devices_LastSeen")]
[Index("OrganizationId", "StatusId", Name = "IX_Devices_Organization_Status")]
[Index("OrganizationId", "DeviceKey", Name = "UQ_Devices_Organization_DeviceKey", IsUnique = true)]
public partial class Device
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(100)]
    public string DeviceKey { get; set; } = null!;

    [StringLength(150)]
    public string MachineName { get; set; } = null!;

    [StringLength(250)]
    public string? MachineSid { get; set; }

    [StringLength(150)]
    public string? OperatingSystem { get; set; }

    [StringLength(100)]
    public string? OsVersion { get; set; }

    [StringLength(150)]
    public string? SerialNumber { get; set; }

    [StringLength(50)]
    public string? MacAddress { get; set; }

    [StringLength(50)]
    public string? AgentVersion { get; set; }

    public int StatusId { get; set; }

    public DateTimeOffset? EnrolledAtUtc { get; set; }

    public DateTimeOffset? LastSeenAtUtc { get; set; }

    public long CurrentPolicyVersion { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    [InverseProperty("Device")]
    public virtual ICollection<AgentCommandResult> AgentCommandResults { get; set; } = new List<AgentCommandResult>();

    [InverseProperty("Device")]
    public virtual ICollection<AgentCommand> AgentCommands { get; set; } = new List<AgentCommand>();

    [InverseProperty("Device")]
    public virtual ICollection<AuditEventBatch> AuditEventBatches { get; set; } = new List<AuditEventBatch>();

    [InverseProperty("Device")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [InverseProperty("Device")]
    public virtual ICollection<DeviceCredential> DeviceCredentials { get; set; } = new List<DeviceCredential>();

    [InverseProperty("Device")]
    public virtual ICollection<DeviceGroupMember> DeviceGroupMembers { get; set; } = new List<DeviceGroupMember>();

    [InverseProperty("Device")]
    public virtual ICollection<DeviceHeartbeat> DeviceHeartbeats { get; set; } = new List<DeviceHeartbeat>();

    [InverseProperty("Device")]
    public virtual DevicePolicyState? DevicePolicyState { get; set; }

    [InverseProperty("Device")]
    public virtual ICollection<DeviceUserAssignment> DeviceUserAssignments { get; set; } = new List<DeviceUserAssignment>();

    [InverseProperty("Device")]
    public virtual ICollection<ObservedFile> ObservedFiles { get; set; } = new List<ObservedFile>();

    [ForeignKey("OrganizationId")]
    [InverseProperty("Devices")]
    public virtual Organization Organization { get; set; } = null!;

    [InverseProperty("TargetDevice")]
    public virtual ICollection<PermissionRequest> PermissionRequests { get; set; } = new List<PermissionRequest>();
    [InverseProperty("TargetDevice")]
    public virtual ICollection<PermissionGrant> PermissionGrants { get; set; } = new List<PermissionGrant>();

    [InverseProperty("Device")]
    public virtual ICollection<SoftwareExecutionEvent> SoftwareExecutionEvents { get; set; } = new List<SoftwareExecutionEvent>();

    [InverseProperty("Device")]
    public virtual ICollection<SoftwareInventory> SoftwareInventories { get; set; } = new List<SoftwareInventory>();

    [ForeignKey("StatusId")]
    [InverseProperty("Devices")]
    public virtual DeviceStatus Status { get; set; } = null!;

    [InverseProperty("Device")]
    public virtual ICollection<UsbDeviceInventory> UsbDeviceInventories { get; set; } = new List<UsbDeviceInventory>();
}
