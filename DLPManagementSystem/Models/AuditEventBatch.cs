using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

[Index("DeviceId", "BatchId", Name = "UQ_AuditEventBatches_Device_Batch", IsUnique = true)]
public partial class AuditEventBatch
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid DeviceId { get; set; }

    public Guid BatchId { get; set; }

    public int EventCount { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; }

    [StringLength(50)]
    public string? AgentVersion { get; set; }

    public long? PolicyVersion { get; set; }

    [InverseProperty("BatchRow")]
    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    [ForeignKey("DeviceId")]
    [InverseProperty("AuditEventBatches")]
    public virtual Device Device { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("AuditEventBatches")]
    public virtual Organization Organization { get; set; } = null!;
}
