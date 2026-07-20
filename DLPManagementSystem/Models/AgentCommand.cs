using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class AgentCommand
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid DeviceId { get; set; }

    [StringLength(100)]
    public string CommandType { get; set; } = null!;

    public string? PayloadJson { get; set; }

    public int StatusId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    [InverseProperty("Command")]
    public virtual ICollection<AgentCommandResult> AgentCommandResults { get; set; } = new List<AgentCommandResult>();

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("AgentCommands")]
    public virtual User CreatedByUser { get; set; } = null!;

    [ForeignKey("DeviceId")]
    [InverseProperty("AgentCommands")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("AgentCommands")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("StatusId")]
    [InverseProperty("AgentCommands")]
    public virtual AgentCommandStatus Status { get; set; } = null!;
}
