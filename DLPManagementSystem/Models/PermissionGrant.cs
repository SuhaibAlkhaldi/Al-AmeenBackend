using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("OrganizationId", "ActionKey", Name = "IX_PermissionGrants_Action")]
[Index("OrganizationId", "SubjectTypeId", "SubjectId", "ActionKey", Name = "IX_PermissionGrants_Subject")]
[Index("OrganizationId","ActionKey","SubjectTypeId","SubjectId","TargetDeviceId","StartsAtUtc","ExpiresAtUtc","RevokedAtUtc",Name = "IX_PermissionGrants_ActiveLookup")]
public partial class PermissionGrant
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(100)]
    public string ActionKey { get; set; } = null!;

    public int DecisionId { get; set; }

    public int SubjectTypeId { get; set; }

    [StringLength(250)]
    public string SubjectId { get; set; } = null!;
    public Guid? TargetDeviceId { get; set; }
    public int GrantTypeId { get; set; }

    public int Priority { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    [StringLength(2000)]
    public string Reason { get; set; } = null!;

    public Guid GrantedByUserId { get; set; }

    public Guid? SourcePermissionRequestId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public Guid? RevokedByUserId { get; set; }

    [StringLength(1000)]
    public string? RevocationReason { get; set; }

    // Both null = ordinary action-level grant (unchanged behavior). FileHash set = grant covers
    // exactly one file (SHA-256, lowercase hex, matches the agent's PermissionGrant.FileHash).
    // ClassificationTier set (FileHash null) = grant covers any file classified at or below this
    // tier. Mutually exclusive - a grant is scoped to one exact file or to a tier, never both.
    [StringLength(64)]
    public string? FileHash { get; set; }

    [StringLength(20)]
    public string? ClassificationTier { get; set; }

    [ForeignKey("ActionKey")]
    [InverseProperty("PermissionGrants")]
    public virtual PermissionAction ActionKeyNavigation { get; set; } = null!;

    [InverseProperty("PermissionGrant")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [ForeignKey("DecisionId")]
    [InverseProperty("PermissionGrants")]
    public virtual PermissionDecision Decision { get; set; } = null!;

    [ForeignKey("GrantTypeId")]
    [InverseProperty("PermissionGrants")]
    public virtual PermissionGrantType GrantType { get; set; } = null!;

    [ForeignKey("GrantedByUserId")]
    [InverseProperty("PermissionGrantGrantedByUsers")]
    public virtual User GrantedByUser { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("PermissionGrants")]
    public virtual Organization Organization { get; set; } = null!;

    [InverseProperty("ResultPermissionGrant")]
    public virtual ICollection<PermissionRequest> PermissionRequests { get; set; } = new List<PermissionRequest>();

    [ForeignKey("RevokedByUserId")]
    [InverseProperty("PermissionGrantRevokedByUsers")]
    public virtual User? RevokedByUser { get; set; }

    [ForeignKey("SourcePermissionRequestId")]
    [InverseProperty("PermissionGrants")]
    public virtual PermissionRequest? SourcePermissionRequest { get; set; }

    [ForeignKey("SubjectTypeId")]
    [InverseProperty("PermissionGrants")]
    public virtual PermissionSubjectType SubjectType { get; set; } = null!;

    [ForeignKey("TargetDeviceId")]
    [InverseProperty("PermissionGrants")]
    public virtual Device? TargetDevice { get; set; }
}
