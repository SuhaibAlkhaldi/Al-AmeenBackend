using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class EmployeeWindowsIdentity
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid EmployeeId { get; set; }

    [StringLength(150)]
    public string? DomainName { get; set; }

    [StringLength(150)]
    public string Username { get; set; } = null!;

    [StringLength(250)]
    public string UserSid { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("EmployeeWindowsIdentities")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("EmployeeWindowsIdentities")]
    public virtual Organization Organization { get; set; } = null!;
}
