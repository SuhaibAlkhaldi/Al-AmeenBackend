using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("ActionKey", "OccurredAtUtc", Name = "IX_AuditEvents_Action_Occurred", IsDescending = new[] { false, true })]
[Index("DeviceId", "OccurredAtUtc", Name = "IX_AuditEvents_Device_Occurred", IsDescending = new[] { false, true })]
[Index("OrganizationId", "OccurredAtUtc", Name = "IX_AuditEvents_Organization_Occurred", IsDescending = new[] { false, true })]
[Index("UserSid", "OccurredAtUtc", Name = "IX_AuditEvents_UserSid_Occurred", IsDescending = new[] { false, true })]
public partial class AuditEvent
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid? BatchRowId { get; set; }

    public Guid DeviceId { get; set; }

    public Guid? EmployeeId { get; set; }

    [StringLength(250)]
    public string? UserSid { get; set; }

    [StringLength(150)]
    public string? Username { get; set; }

    [StringLength(100)]
    public string ActionKey { get; set; } = null!;

    public int EventTypeId { get; set; }

    public int DecisionId { get; set; }

    public int? ReasonCodeId { get; set; }

    public Guid? PermissionGrantId { get; set; }

    public Guid? ObservedFileId { get; set; }

    public long? PolicyVersion { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    [StringLength(50)]
    public string? AgentVersion { get; set; }

    public Guid? CorrelationId { get; set; }

    public string? MetadataJson { get; set; }

    [ForeignKey("ActionKey")]
    [InverseProperty("AuditEvents")]
    public virtual PermissionAction ActionKeyNavigation { get; set; } = null!;

    [InverseProperty("AuditEvent")]
    public virtual ICollection<AiAnalysisResult> AiAnalysisResults { get; set; } = new List<AiAnalysisResult>();

    [InverseProperty("AuditEvent")]
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    [ForeignKey("BatchRowId")]
    [InverseProperty("AuditEvents")]
    public virtual AuditEventBatch? BatchRow { get; set; }

    [ForeignKey("DecisionId")]
    [InverseProperty("AuditEvents")]
    public virtual AuditDecision Decision { get; set; } = null!;

    [ForeignKey("DeviceId")]
    [InverseProperty("AuditEvents")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    [InverseProperty("AuditEvents")]
    public virtual Employee? Employee { get; set; }

    [ForeignKey("EventTypeId")]
    [InverseProperty("AuditEvents")]
    public virtual AuditEventType EventType { get; set; } = null!;

    [ForeignKey("ObservedFileId")]
    [InverseProperty("AuditEvents")]
    public virtual ObservedFile? ObservedFile { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("AuditEvents")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("PermissionGrantId")]
    [InverseProperty("AuditEvents")]
    public virtual PermissionGrant? PermissionGrant { get; set; }

    [ForeignKey("ReasonCodeId")]
    [InverseProperty("AuditEvents")]
    public virtual AuditReasonCode? ReasonCode { get; set; }
}
