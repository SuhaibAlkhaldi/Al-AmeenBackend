using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class CreatePermissionRequestDto
    {
        [Required]
        [StringLength(100)]
        public string ActionKey { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string GrantType { get; set; } = string.Empty;
        public DateTimeOffset? RequestedStartsAtUtc { get; set; }

        public DateTimeOffset? RequestedExpiresAtUtc { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 5)]
        public string BusinessJustification { get; set; } = string.Empty;

        // Exact-file request path: the agent-reported AuditEvent for a real blocked upload/drag-drop
        // attempt. File details (hash/name/size/classification/reasoning) are never trusted from the
        // client - the server looks them up from this event's own MetadataJson. Mutually exclusive
        // with ClassificationTier below.
        public Guid? SourceCorrelationId { get; set; }

        // Tier-scoped request path: the employee picked a classification tier instead of a specific
        // file. Must be one of ClassificationTiers.Internal/Secret/VerySecret (never Public, which
        // never needs a request). Mutually exclusive with SourceCorrelationId above.
        [StringLength(20)]
        public string? ClassificationTier { get; set; }
    }
}
