using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class AiAnalysisOverride
{
    [Key]
    public Guid Id { get; set; }

    public Guid AiAnalysisResultId { get; set; }

    public Guid AdminUserId { get; set; }

    public int DecisionId { get; set; }

    [StringLength(2000)]
    public string Reason { get; set; } = null!;

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public bool IsTemporary { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    [ForeignKey("AdminUserId")]
    [InverseProperty("AiAnalysisOverrides")]
    public virtual User AdminUser { get; set; } = null!;

    [ForeignKey("AiAnalysisResultId")]
    [InverseProperty("AiAnalysisOverrides")]
    public virtual AiAnalysisResult AiAnalysisResult { get; set; } = null!;

    [ForeignKey("DecisionId")]
    [InverseProperty("AiAnalysisOverrides")]
    public virtual AuditDecision Decision { get; set; } = null!;
}
