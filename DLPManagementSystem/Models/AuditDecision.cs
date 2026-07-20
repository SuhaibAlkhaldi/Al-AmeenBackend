using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Name", Name = "UQ_AuditDecisions_Name", IsUnique = true)]
public partial class AuditDecision
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    [InverseProperty("Decision")]
    public virtual ICollection<AiAnalysisOverride> AiAnalysisOverrides { get; set; } = new List<AiAnalysisOverride>();

    [InverseProperty("Decision")]
    public virtual ICollection<AiAnalysisResult> AiAnalysisResults { get; set; } = new List<AiAnalysisResult>();

    [InverseProperty("Decision")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [InverseProperty("Decision")]
    public virtual ICollection<SoftwareExecutionEvent> SoftwareExecutionEvents { get; set; } = new List<SoftwareExecutionEvent>();
}
