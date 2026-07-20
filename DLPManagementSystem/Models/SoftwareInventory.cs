using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Table("SoftwareInventory")]
public partial class SoftwareInventory
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid DeviceId { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(255)]
    public string? Publisher { get; set; }

    [StringLength(100)]
    public string? Version { get; set; }

    [StringLength(1000)]
    public string? InstallPath { get; set; }

    [StringLength(1000)]
    public string? ExecutablePath { get; set; }

    [StringLength(128)]
    public string? FileHash { get; set; }

    public DateTimeOffset FirstSeenAtUtc { get; set; }

    public DateTimeOffset? LastSeenAtUtc { get; set; }

    public bool IsApproved { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("SoftwareInventories")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("SoftwareInventories")]
    public virtual Organization Organization { get; set; } = null!;
}
