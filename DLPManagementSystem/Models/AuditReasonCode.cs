using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Code", Name = "UQ_AuditReasonCodes_Code", IsUnique = true)]
public partial class AuditReasonCode
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Code { get; set; } = null!;

    [StringLength(150)]
    public string DisplayName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [InverseProperty("ReasonCode")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [InverseProperty("ReasonCode")]
    public virtual ICollection<SoftwareExecutionEvent> SoftwareExecutionEvents { get; set; } = new List<SoftwareExecutionEvent>();
}
