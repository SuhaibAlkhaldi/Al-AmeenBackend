using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class AiAnalysisResult
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AuditEventId { get; set; }

    public int DecisionId { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal RiskScore { get; set; }

    [StringLength(2000)]
    public string? Reason { get; set; }

    [StringLength(150)]
    public string? EngineName { get; set; }

    [StringLength(50)]
    public string? EvaluationVersion { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? ConfidenceScore { get; set; }

    public int? ProcessingTimeMs { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    [InverseProperty("AiAnalysisResult")]
    public virtual ICollection<AiAnalysisOverride> AiAnalysisOverrides { get; set; } = new List<AiAnalysisOverride>();

    [ForeignKey("AuditEventId")]
    [InverseProperty("AiAnalysisResults")]
    public virtual AuditEvent AuditEvent { get; set; } = null!;

    [ForeignKey("DecisionId")]
    [InverseProperty("AiAnalysisResults")]
    public virtual AuditDecision Decision { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("AiAnalysisResults")]
    public virtual Organization Organization { get; set; } = null!;
}
