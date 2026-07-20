using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class PermissionAction
{
    [Key]
    [StringLength(100)]
    public string Key { get; set; } = null!;

    public int CategoryId { get; set; }

    [StringLength(150)]
    public string DisplayName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public int DefaultDecisionId { get; set; }

    public bool SupportsPermanentGrant { get; set; }

    public bool SupportsTemporaryGrant { get; set; }

    public bool IsEnabled { get; set; }

    public int SortOrder { get; set; }

    [InverseProperty("ActionKeyNavigation")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [ForeignKey("CategoryId")]
    [InverseProperty("PermissionActions")]
    public virtual PermissionActionCategory Category { get; set; } = null!;

    [ForeignKey("DefaultDecisionId")]
    [InverseProperty("PermissionActions")]
    public virtual PermissionDecision DefaultDecision { get; set; } = null!;

    [InverseProperty("ActionKeyNavigation")]
    public virtual ICollection<PermissionGrant> PermissionGrants { get; set; } = new List<PermissionGrant>();

    [InverseProperty("ActionKeyNavigation")]
    public virtual ICollection<PermissionRequest> PermissionRequests { get; set; } = new List<PermissionRequest>();
}
