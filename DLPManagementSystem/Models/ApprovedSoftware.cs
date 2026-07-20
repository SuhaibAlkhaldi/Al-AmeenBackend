using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Table("ApprovedSoftware")]
public partial class ApprovedSoftware
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(255)]
    public string? Publisher { get; set; }

    [StringLength(128)]
    public string? FileHash { get; set; }

    [StringLength(500)]
    public string? VersionRule { get; set; }

    public bool IsActive { get; set; }

    public Guid ApprovedByUserId { get; set; }

    public DateTimeOffset ApprovedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    [ForeignKey("ApprovedByUserId")]
    [InverseProperty("ApprovedSoftwares")]
    public virtual User ApprovedByUser { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("ApprovedSoftwares")]
    public virtual Organization Organization { get; set; } = null!;
}
