using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("Name", Name = "UQ_AuditEventTypes_Name", IsUnique = true)]
public partial class AuditEventType
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(150)]
    public string DisplayName { get; set; } = null!;

    [InverseProperty("EventType")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
}
