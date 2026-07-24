using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("OrganizationId", "OccurredAtUtc", Name = "IX_AdminAuditLogs_Organization_Occurred", IsDescending = new[] { false, true })]
[Index("AdminUserId", "OccurredAtUtc", Name = "IX_AdminAuditLogs_AdminUser_Occurred", IsDescending = new[] { false, true })]
public partial class AdminAuditLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AdminUserId { get; set; }

    // Denormalized snapshot of the actor at write time, captured alongside the AdminUserId FK, so
    // this log stays readable (who, and what role they held) even after the actor User row is
    // later deleted or its role changes.
    [StringLength(150)]
    public string ActorEmail { get; set; } = null!;

    [StringLength(150)]
    public string ActorFullName { get; set; } = null!;

    [StringLength(100)]
    public string ActorRoleName { get; set; } = null!;

    [StringLength(150)]
    public string Action { get; set; } = null!;

    [StringLength(100)]
    public string EntityType { get; set; } = null!;

    public Guid? EntityId { get; set; }

    // Denormalized display name of the affected target (e.g. the employee's name), so the log reads
    // clearly in a list without joining back to the (possibly since-deleted) target row.
    [StringLength(250)]
    public string? TargetDisplayName { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public string? MetadataJson { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    [ForeignKey("AdminUserId")]
    [InverseProperty("AdminAuditLogs")]
    public virtual User AdminUser { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("AdminAuditLogs")]
    public virtual Organization Organization { get; set; } = null!;
}
