namespace DLPManagementSystem.DTO.AdminAuditLogs
{
    public class AdminAuditLogListItemDto
    {
        public Guid Id { get; set; }

        public DateTimeOffset OccurredAtUtc { get; set; }

        public Guid ActorUserId { get; set; }

        public string ActorEmail { get; set; } = string.Empty;

        public string ActorFullName { get; set; } = string.Empty;

        public string ActorRoleName { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public Guid? TargetId { get; set; }

        public string? TargetDisplayName { get; set; }

        public string? Details { get; set; }
    }
}
