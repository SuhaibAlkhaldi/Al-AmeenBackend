namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class PermissionGrantDto
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public string ActionKey { get; set; } = string.Empty;

        public string Decision { get; set; } = string.Empty;

        public string SubjectType { get; set; } = string.Empty;

        public string SubjectId { get; set; } = string.Empty;

        public string? SubjectDisplayName { get; set; }

        public Guid? TargetDeviceId { get; set; }

        public string? TargetDeviceName { get; set; }

        public string GrantType { get; set; } = string.Empty;

        public int Priority { get; set; }

        public DateTimeOffset StartsAtUtc { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }

        public string RuntimeStatus { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public Guid GrantedByUserId { get; set; }

        public string? GrantedByUserName { get; set; }

        public Guid? SourcePermissionRequestId { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset? RevokedAtUtc { get; set; }

        public string? RevocationReason { get; set; }

        public string? RevokedByUserName { get; set; }
    }
}
