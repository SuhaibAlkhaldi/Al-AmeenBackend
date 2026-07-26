namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class PermissionRequestDto
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public Guid RequestedByUserId { get; set; }

        public Guid RequestedByEmployeeId { get; set; }

        public string? RequestedByEmployeeName { get; set; }

        public string ActionKey { get; set; } = string.Empty;

        public string RequestedDecision { get; set; } = string.Empty;

        public string RequestedGrantType { get; set; } = string.Empty;

        public string SubjectType { get; set; } = string.Empty;

        public string SubjectId { get; set; } = string.Empty;

        public Guid? TargetDeviceId { get; set; }

        public string? TargetDeviceName { get; set; }

        public DateTimeOffset? RequestedStartsAtUtc { get; set; }

        public DateTimeOffset? RequestedExpiresAtUtc { get; set; }

        public int? RequestedDurationMinutes { get; set; }

        public string BusinessJustification { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset? SubmittedAtUtc { get; set; }

        public Guid? ReviewedByUserId { get; set; }

        public string? ReviewedByUserName { get; set; }

        public DateTimeOffset? ReviewedAtUtc { get; set; }

        public string? ReviewDecision { get; set; }

        public string? ReviewNotes { get; set; }

        public Guid? ResultPermissionGrantId { get; set; }

        // Actual terms of the resulting grant, when one exists — may differ from RequestedGrantType/
        // RequestedExpiresAtUtc above if the admin changed them at approval time.
        public string? GrantedGrantType { get; set; }

        public DateTimeOffset? GrantedStartsAtUtc { get; set; }

        public DateTimeOffset? GrantedExpiresAtUtc { get; set; }

        public string? GrantRuntimeStatus { get; set; }

        public DateTimeOffset? GrantRevokedAtUtc { get; set; }

        public string? GrantRevocationReason { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset? UpdatedAtUtc { get; set; }

        // Tier path: set when this request targeted a classification tier rather than a specific file.
        public string? ClassificationTier { get; set; }

        // Exact-file path: populated from the request's linked PermissionRequestAttachment (created
        // server-side from the source AuditEvent in CreateAsync). Null for both the tier path and
        // ordinary action-level requests.
        public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? FileSha256 { get; set; }
        public string? FileClassification { get; set; }
        public string? FileClassificationReasonCode { get; set; }
    }
}
