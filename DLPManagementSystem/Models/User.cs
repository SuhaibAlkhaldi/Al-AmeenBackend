using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("OrganizationId", "RoleId", Name = "IX_Users_Organization_Role")]
[Index("OrganizationId", "Email", Name = "UQ_Users_Organization_Email", IsUnique = true)]
public partial class User
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(150)]
    public string FullName { get; set; } = null!;

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [StringLength(500)]
    public string PasswordHash { get; set; } = null!;

    public int UserTypeId { get; set; }

    public int RoleId { get; set; }

    public int StatusId { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public int FailedLoginAttemptCount { get; set; }

    public DateTimeOffset? LockedOutUntilUtc { get; set; }

    public bool IsEmailVerified { get; set; }

    // True whenever this account's current password was set by someone other than the account
    // holder (an admin-typed or auto-generated password at creation, or an admin-initiated
    // reset) - the frontend forces a redirect to the change-password page until it's cleared.
    // Defaults true so every newly constructed User starts in that state unless a call site
    // (the dev seeder's known/shared credentials) explicitly opts out.
    public bool MustChangePassword { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    [InverseProperty("AdminUser")]
    public virtual ICollection<AdminAuditLog> AdminAuditLogs { get; set; } = new List<AdminAuditLog>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<AgentCommand> AgentCommands { get; set; } = new List<AgentCommand>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<AgentEnrollmentToken> AgentEnrollmentTokens { get; set; } = new List<AgentEnrollmentToken>();

    [InverseProperty("AdminUser")]
    public virtual ICollection<AiAnalysisOverride> AiAnalysisOverrides { get; set; } = new List<AiAnalysisOverride>();

    [InverseProperty("AssignedToUser")]
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    [InverseProperty("ApprovedByUser")]
    public virtual ICollection<ApprovedSoftware> ApprovedSoftwares { get; set; } = new List<ApprovedSoftware>();

    [InverseProperty("AddedByUser")]
    public virtual ICollection<DeviceGroupMember> DeviceGroupMembers { get; set; } = new List<DeviceGroupMember>();

    [InverseProperty("AssignedByUser")]
    public virtual ICollection<DeviceUserAssignment> DeviceUserAssignments { get; set; } = new List<DeviceUserAssignment>();

    [InverseProperty("User")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    [ForeignKey("OrganizationId")]
    [InverseProperty("Users")]
    public virtual Organization Organization { get; set; } = null!;

    [InverseProperty("GrantedByUser")]
    public virtual ICollection<PermissionGrant> PermissionGrantGrantedByUsers { get; set; } = new List<PermissionGrant>();

    [InverseProperty("RevokedByUser")]
    public virtual ICollection<PermissionGrant> PermissionGrantRevokedByUsers { get; set; } = new List<PermissionGrant>();

    [InverseProperty("UploadedByUser")]
    public virtual ICollection<PermissionRequestAttachment> PermissionRequestAttachments { get; set; } = new List<PermissionRequestAttachment>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<PermissionRequestComment> PermissionRequestComments { get; set; } = new List<PermissionRequestComment>();

    [InverseProperty("ChangedByUser")]
    public virtual ICollection<PermissionRequestHistory> PermissionRequestHistories { get; set; } = new List<PermissionRequestHistory>();

    [InverseProperty("RequestedByUser")]
    public virtual ICollection<PermissionRequest> PermissionRequestRequestedByUsers { get; set; } = new List<PermissionRequest>();

    [InverseProperty("ReviewedByUser")]
    public virtual ICollection<PermissionRequest> PermissionRequestReviewedByUsers { get; set; } = new List<PermissionRequest>();

    [InverseProperty("ChangedByUser")]
    public virtual ICollection<PolicyChangeLog> PolicyChangeLogs { get; set; } = new List<PolicyChangeLog>();

    [InverseProperty("ChangedByUser")]
    public virtual ICollection<PolicyVersion> PolicyVersions { get; set; } = new List<PolicyVersion>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("StatusId")]
    [InverseProperty("Users")]
    public virtual UserStatus Status { get; set; } = null!;

    [InverseProperty("ApprovedByUser")]
    public virtual ICollection<UsbDeviceApproval> UsbDeviceApprovals { get; set; } = new List<UsbDeviceApproval>();

    [ForeignKey("UserTypeId")]
    [InverseProperty("Users")]
    public virtual UserType UserType { get; set; } = null!;
}
