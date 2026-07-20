using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class DeviceCredential
{
    [Key]
    public Guid Id { get; set; }

    public Guid DeviceId { get; set; }

    [StringLength(500)]
    public string SecretHash { get; set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastUsedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public DateTimeOffset? RotationDueAtUtc { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("DeviceCredentials")]
    public virtual Device Device { get; set; } = null!;
}
