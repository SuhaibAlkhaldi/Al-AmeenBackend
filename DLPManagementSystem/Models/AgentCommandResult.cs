using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class AgentCommandResult
{
    [Key]
    public Guid Id { get; set; }

    public Guid CommandId { get; set; }

    public Guid DeviceId { get; set; }

    public bool Success { get; set; }

    [StringLength(2000)]
    public string? ResultMessage { get; set; }

    public string? ResultJson { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    [ForeignKey("CommandId")]
    [InverseProperty("AgentCommandResults")]
    public virtual AgentCommand Command { get; set; } = null!;

    [ForeignKey("DeviceId")]
    [InverseProperty("AgentCommandResults")]
    public virtual Device Device { get; set; } = null!;
}
