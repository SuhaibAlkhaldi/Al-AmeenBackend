using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

public partial class Alert
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AuditEventId { get; set; }

    public int AlertLevelId { get; set; }

    public int AlertStatusId { get; set; }

    [StringLength(255)]
    public string Title { get; set; } = null!;

    [StringLength(2000)]
    public string? Description { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ClosedAtUtc { get; set; }

    public string? InvestigationNotes { get; set; }

    public bool IsFalsePositive { get; set; }

    public DateTimeOffset? EmailNotifiedAtUtc { get; set; }

    [ForeignKey("AlertLevelId")]
    [InverseProperty("Alerts")]
    public virtual AlertLevel AlertLevel { get; set; } = null!;

    [ForeignKey("AlertStatusId")]
    [InverseProperty("Alerts")]
    public virtual AlertStatus AlertStatus { get; set; } = null!;

    [ForeignKey("AssignedToUserId")]
    [InverseProperty("Alerts")]
    public virtual User? AssignedToUser { get; set; }

    [ForeignKey("AuditEventId")]
    [InverseProperty("Alerts")]
    public virtual AuditEvent AuditEvent { get; set; } = null!;

    [ForeignKey("OrganizationId")]
    [InverseProperty("Alerts")]
    public virtual Organization Organization { get; set; } = null!;
}
