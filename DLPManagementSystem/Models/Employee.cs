using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("OrganizationId", "StatusId", Name = "IX_Employees_Organization_Status")]
[Index("OrganizationId", "Email", Name = "UQ_Employees_Organization_Email", IsUnique = true)]
[Index("OrganizationId", "EmployeeNumber", Name = "UQ_Employees_Organization_EmployeeNumber", IsUnique = true)]
[Index("OrganizationId", "UserId", Name = "UQ_Employees_Organization_User", IsUnique = true)]
public partial class Employee
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    public Guid? DepartmentId { get; set; }

    [StringLength(50)]
    public string EmployeeNumber { get; set; } = null!;

    [StringLength(150)]
    public string DisplayName { get; set; } = null!;

    [StringLength(255)]
    public string Email { get; set; } = null!;

    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    public int StatusId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [ForeignKey("DepartmentId")]
    [InverseProperty("Employees")]
    public virtual Department? Department { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<DeviceUserAssignment> DeviceUserAssignments { get; set; } = new List<DeviceUserAssignment>();

    [InverseProperty("Employee")]
    public virtual ICollection<EmployeeWindowsIdentity> EmployeeWindowsIdentities { get; set; } = new List<EmployeeWindowsIdentity>();

    [ForeignKey("OrganizationId")]
    [InverseProperty("Employees")]
    public virtual Organization Organization { get; set; } = null!;

    [InverseProperty("RequestedByEmployee")]
    public virtual ICollection<PermissionRequest> PermissionRequests { get; set; } = new List<PermissionRequest>();

    [ForeignKey("StatusId")]
    [InverseProperty("Employees")]
    public virtual EmployeeStatus Status { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Employees")]
    public virtual User User { get; set; } = null!;
}
