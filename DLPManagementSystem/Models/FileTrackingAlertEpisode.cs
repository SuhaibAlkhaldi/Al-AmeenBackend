using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Models;

// One continuous burst of a rule firing for one content fingerprint (File Tracking plan, Phase 5). A burst
// raises exactly one alert; while it keeps qualifying, only EpisodeLastObservedAtUtc advances. A new episode
// (and a new alert) starts only after a quiet gap longer than the rule's window.
//
// Identity: (OrganizationId, ContentFingerprint, RuleType, EpisodeAnchorAtUtc) is unique, so a repeated worker
// run over the same data can never create a second row for the same burst.
[Index("OrganizationId", "ContentFingerprint", "RuleType", "EpisodeAnchorAtUtc", Name = "UX_FileTrackingAlertEpisodes_Burst", IsUnique = true)]
[Index("OrganizationId", "ContentFingerprint", "RuleType", "EpisodeLastObservedAtUtc", Name = "IX_FileTrackingAlertEpisodes_Open")]
public partial class FileTrackingAlertEpisode
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    [StringLength(64)]
    public string ContentFingerprint { get; set; } = null!;

    [StringLength(32)]
    public string RuleType { get; set; } = null!;

    public DateTimeOffset EpisodeAnchorAtUtc { get; set; }

    public DateTimeOffset EpisodeLastObservedAtUtc { get; set; }

    public int DistinctEmployeeCount { get; set; }

    public int DistinctDeviceCount { get; set; }

    public Guid AuditEventId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey("AuditEventId")]
    public virtual AuditEvent AuditEvent { get; set; } = null!;
}
