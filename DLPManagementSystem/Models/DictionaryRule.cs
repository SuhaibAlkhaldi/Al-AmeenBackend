using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// Stores the whole admin-configured classification rule set as JSON, versioned - a new save
// deactivates the previous active row and inserts a new one, rather than editing rules in place, so
// the evaluator (both the agent-side C# port and this project's own reference behavior) always
// considers a single, atomic, current rule set.
[Index("OrganizationId", "IsActive", Name = "IX_DictionaryRules_ActiveLookup")]
public partial class DictionaryRule
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public long Version { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string RulesJson { get; set; } = "[]";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    [ForeignKey("OrganizationId")]
    [InverseProperty("DictionaryRules")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("CreatedByUserId")]
    public virtual User CreatedByUser { get; set; } = null!;
}
