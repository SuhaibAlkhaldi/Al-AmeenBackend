using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class SoftwareExecutionEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid DeviceId { get; set; }

    [StringLength(250)]
    public string? UserSid { get; set; }

    [StringLength(255)]
    public string ProcessName { get; set; } = null!;

    [StringLength(1000)]
    public string? ExecutablePath { get; set; }

    [StringLength(128)]
    public string? FileHash { get; set; }

    [StringLength(255)]
    public string? Publisher { get; set; }

    public int DecisionId { get; set; }

    public int? ReasonCodeId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    public string? MetadataJson { get; set; }

    [ForeignKey("DecisionId")]
    [InverseProperty("SoftwareExecutionEvents")]
    public virtual AuditDecision Decision { get; set; } = null!;

    [ForeignKey("DeviceId")]
    [InverseProperty("SoftwareExecutionEvents")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("SoftwareExecutionEvents")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("ReasonCodeId")]
    [InverseProperty("SoftwareExecutionEvents")]
    public virtual AuditReasonCode? ReasonCode { get; set; }
}
