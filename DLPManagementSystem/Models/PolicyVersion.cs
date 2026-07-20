using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("OrganizationId", "ChangedAtUtc", Name = "IX_PolicyVersions_Organization_Changed", IsDescending = new[] { false, true })]
[Index("OrganizationId", "VersionNumber", Name = "UQ_PolicyVersions_Organization_Version", IsUnique = true)]
public partial class PolicyVersion
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public long VersionNumber { get; set; }

    public DateTimeOffset ChangedAtUtc { get; set; }

    public Guid? ChangedByUserId { get; set; }

    [StringLength(1000)]
    public string ChangeReason { get; set; } = null!;

    [ForeignKey("ChangedByUserId")]
    [InverseProperty("PolicyVersions")]
    public virtual User? ChangedByUser { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("PolicyVersions")]
    public virtual Organization Organization { get; set; } = null!;

    [InverseProperty("PolicyVersion")]
    public virtual ICollection<PolicyChangeLog> PolicyChangeLogs { get; set; } = new List<PolicyChangeLog>();
}
