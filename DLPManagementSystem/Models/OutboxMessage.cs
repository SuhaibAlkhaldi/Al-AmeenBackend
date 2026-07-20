using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class OutboxMessage
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(200)]
    public string Type { get; set; } = null!;

    public string PayloadJson { get; set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }
}
