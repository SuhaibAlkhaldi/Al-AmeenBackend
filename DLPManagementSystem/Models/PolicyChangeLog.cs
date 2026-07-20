using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class PolicyChangeLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid PolicyVersionId { get; set; }

    [StringLength(100)]
    public string ChangeType { get; set; } = null!;

    [StringLength(100)]
    public string EntityType { get; set; } = null!;

    public Guid? EntityId { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public DateTimeOffset ChangedAtUtc { get; set; }

    public string? MetadataJson { get; set; }

    [ForeignKey("ChangedByUserId")]
    [InverseProperty("PolicyChangeLogs")]
    public virtual User? ChangedByUser { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("PolicyChangeLogs")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("PolicyVersionId")]
    [InverseProperty("PolicyChangeLogs")]
    public virtual PolicyVersion PolicyVersion { get; set; } = null!;
}
