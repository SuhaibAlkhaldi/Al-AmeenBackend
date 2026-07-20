using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class AgentEnrollmentToken
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(500)]
    public string TokenHash { get; set; } = null!;

    [StringLength(150)]
    public string DisplayName { get; set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public int MaxUses { get; set; }

    public int UsedCount { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("AgentEnrollmentTokens")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("AgentEnrollmentTokens")]
    public virtual Organization Organization { get; set; } = null!;
}
