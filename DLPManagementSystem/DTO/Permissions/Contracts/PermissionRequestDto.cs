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

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }
}
