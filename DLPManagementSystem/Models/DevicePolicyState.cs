using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class DevicePolicyState
{
    [Key]
    public Guid DeviceId { get; set; }

    public Guid OrganizationId { get; set; }

    public long? LastFetchedPolicyVersion { get; set; }

    public DateTimeOffset? LastFetchedAtUtc { get; set; }

    public long? LastAppliedPolicyVersion { get; set; }

    public DateTimeOffset? LastAppliedAtUtc { get; set; }

    [StringLength(128)]
    public string? LastPolicyHash { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("DevicePolicyState")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("DevicePolicyStates")]
    public virtual Organization Organization { get; set; } = null!;
}
