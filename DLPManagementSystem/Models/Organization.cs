using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Code", Name = "UQ_Organizations_Code", IsUnique = true)]
public partial class Organization
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(50)]
    public string Code { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    [InverseProperty("Organization")]
    public virtual ICollection<AdminAuditLog> AdminAuditLogs { get; set; } = new List<AdminAuditLog>();

    [InverseProperty("Organization")]
    public virtual ICollection<AgentCommand> AgentCommands { get; set; } = new List<AgentCommand>();

    [InverseProperty("Organization")]
    public virtual ICollection<AgentEnrollmentToken> AgentEnrollmentTokens { get; set; } = new List<AgentEnrollmentToken>();

    [InverseProperty("Organization")]
    public virtual ICollection<AiAnalysisResult> AiAnalysisResults { get; set; } = new List<AiAnalysisResult>();

    [InverseProperty("Organization")]
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    [InverseProperty("Organization")]
    public virtual ICollection<ApprovedSoftware> ApprovedSoftwares { get; set; } = new List<ApprovedSoftware>();

    [InverseProperty("Organization")]
    public virtual ICollection<AuditEventBatch> AuditEventBatches { get; set; } = new List<AuditEventBatch>();

    [InverseProperty("Organization")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [InverseProperty("Organization")]
    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    [InverseProperty("Organization")]
    public virtual ICollection<DeviceGroup> DeviceGroups { get; set; } = new List<DeviceGroup>();

    [InverseProperty("Organization")]
    public virtual ICollection<DeviceHeartbeat> DeviceHeartbeats { get; set; } = new List<DeviceHeartbeat>();

    [InverseProperty("Organization")]
    public virtual ICollection<DevicePolicyState> DevicePolicyStates { get; set; } = new List<DevicePolicyState>();

    [InverseProperty("Organization")]
    public virtual ICollection<DeviceUserAssignment> DeviceUserAssignments { get; set; } = new List<DeviceUserAssignment>();

    [InverseProperty("Organization")]
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    [InverseProperty("Organization")]
    public virtual ICollection<EmployeeWindowsIdentity> EmployeeWindowsIdentities { get; set; } = new List<EmployeeWindowsIdentity>();

    [InverseProperty("Organization")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    [InverseProperty("Organization")]
    public virtual ICollection<ObservedFile> ObservedFiles { get; set; } = new List<ObservedFile>();

    [InverseProperty("Organization")]
    public virtual ICollection<PermissionGrant> PermissionGrants { get; set; } = new List<PermissionGrant>();

    [InverseProperty("Organization")]
    public virtual ICollection<PermissionRequestAttachment> PermissionRequestAttachments { get; set; } = new List<PermissionRequestAttachment>();

    [InverseProperty("Organization")]
    public virtual ICollection<PermissionRequestComment> PermissionRequestComments { get; set; } = new List<PermissionRequestComment>();

    [InverseProperty("Organization")]
    public virtual ICollection<PermissionRequestHistory> PermissionRequestHistories { get; set; } = new List<PermissionRequestHistory>();

    [InverseProperty("Organization")]
    public virtual ICollection<PermissionRequest> PermissionRequests { get; set; } = new List<PermissionRequest>();

    [InverseProperty("Organization")]
    public virtual ICollection<PolicyChangeLog> PolicyChangeLogs { get; set; } = new List<PolicyChangeLog>();

    [InverseProperty("Organization")]
    public virtual ICollection<PolicyVersion> PolicyVersions { get; set; } = new List<PolicyVersion>();

    [InverseProperty("Organization")]
    public virtual ICollection<SoftwareExecutionEvent> SoftwareExecutionEvents { get; set; } = new List<SoftwareExecutionEvent>();

    [InverseProperty("Organization")]
    public virtual ICollection<SoftwareInventory> SoftwareInventories { get; set; } = new List<SoftwareInventory>();

    [InverseProperty("Organization")]
    public virtual ICollection<UsbDeviceApproval> UsbDeviceApprovals { get; set; } = new List<UsbDeviceApproval>();

    [InverseProperty("Organization")]
    public virtual ICollection<UsbDeviceInventory> UsbDeviceInventories { get; set; } = new List<UsbDeviceInventory>();

    [InverseProperty("Organization")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
