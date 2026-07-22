using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("OrganizationId", "StatusId", "CreatedAtUtc", Name = "IX_PermissionRequests_Organization_Status", IsDescending = new[] { false, false, true })]
[Index("RequestedByUserId", "CreatedAtUtc", Name = "IX_PermissionRequests_RequestedByUser", IsDescending = new[] { false, true })]
public partial class PermissionRequest
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid RequestedByUserId { get; set; }

    public Guid RequestedByEmployeeId { get; set; }

    [StringLength(100)]
    public string ActionKey { get; set; } = null!;

    public int RequestedDecisionId { get; set; }

    public int RequestedGrantTypeId { get; set; }

    public int SubjectTypeId { get; set; }

    [StringLength(250)]
    public string SubjectId { get; set; } = null!;

    public Guid? TargetDeviceId { get; set; }

    public DateTimeOffset? RequestedStartsAtUtc { get; set; }

    public DateTimeOffset? RequestedExpiresAtUtc { get; set; }

    public int? RequestedDurationMinutes { get; set; }

    [StringLength(2000)]
    public string BusinessJustification { get; set; } = null!;

    public int StatusId { get; set; }

    public DateTimeOffset? SubmittedAtUtc { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAtUtc { get; set; }

    public int? ReviewDecisionId { get; set; }

    [StringLength(2000)]
    public string? ReviewNotes { get; set; }

    public Guid? ResultPermissionGrantId { get; set; }

    public DateTimeOffset? CancelledAtUtc { get; set; }

    [StringLength(1000)]
    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    [ForeignKey("ActionKey")]
    [InverseProperty("PermissionRequests")]
    public virtual PermissionAction ActionKeyNavigation { get; set; } = null!;

    [InverseProperty("PermissionRequest")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [ForeignKey("OrganizationId")]
    [InverseProperty("PermissionRequests")]
    public virtual Organization Organization { get; set; } = null!;

    [InverseProperty("SourcePermissionRequest")]
    public virtual ICollection<PermissionGrant> PermissionGrants { get; set; } = new List<PermissionGrant>();

    [InverseProperty("PermissionRequest")]
    public virtual ICollection<PermissionRequestAttachment> PermissionRequestAttachments { get; set; } = new List<PermissionRequestAttachment>();

    [InverseProperty("PermissionRequest")]
    public virtual ICollection<PermissionRequestComment> PermissionRequestComments { get; set; } = new List<PermissionRequestComment>();

    [InverseProperty("PermissionRequest")]
    public virtual ICollection<PermissionRequestHistory> PermissionRequestHistories { get; set; } = new List<PermissionRequestHistory>();

    [ForeignKey("RequestedByEmployeeId")]
    [InverseProperty("PermissionRequests")]
    public virtual Employee RequestedByEmployee { get; set; } = null!;

    [ForeignKey("RequestedByUserId")]
    [InverseProperty("PermissionRequestRequestedByUsers")]
    public virtual User RequestedByUser { get; set; } = null!;

    [ForeignKey("RequestedDecisionId")]
    [InverseProperty("PermissionRequests")]
    public virtual PermissionDecision RequestedDecision { get; set; } = null!;

    [ForeignKey("RequestedGrantTypeId")]
    [InverseProperty("PermissionRequests")]
    public virtual PermissionGrantType RequestedGrantType { get; set; } = null!;

    [ForeignKey("ResultPermissionGrantId")]
    [InverseProperty("PermissionRequests")]
    public virtual PermissionGrant? ResultPermissionGrant { get; set; }

    [ForeignKey("ReviewDecisionId")]
    [InverseProperty("PermissionRequests")]
    public virtual PermissionRequestReviewDecision? ReviewDecision { get; set; }

    [ForeignKey("ReviewedByUserId")]
    [InverseProperty("PermissionRequestReviewedByUsers")]
    public virtual User? ReviewedByUser { get; set; }

    [ForeignKey("StatusId")]
    [InverseProperty("PermissionRequests")]
    public virtual PermissionRequestStatus Status { get; set; } = null!;

    [ForeignKey("SubjectTypeId")]
    [InverseProperty("PermissionRequests")]
    public virtual PermissionSubjectType SubjectType { get; set; } = null!;

    [ForeignKey("TargetDeviceId")]
    [InverseProperty("PermissionRequests")]
    public virtual Device? TargetDevice { get; set; }
}
