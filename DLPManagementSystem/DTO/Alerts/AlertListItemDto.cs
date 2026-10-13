namespace DLPManagementSystem.DTO.Alerts
{
    public class AlertListItemDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public int AlertLevelId { get; set; }

        public string AlertLevelName { get; set; } = string.Empty;

        public int AlertStatusId { get; set; }

        public string AlertStatusName { get; set; } = string.Empty;

        public string DeviceName { get; set; } = string.Empty;

        // The triggering audit event's action category (e.g. "USB", "File", "Browser") - derived
        // from PermissionAction.Category via AuditEvent.ActionKeyNavigation, the same lookup chain
        // the dashboard's ChannelBreakdown already uses. Null only if the underlying ActionKey has
        // somehow lost its category join (shouldn't happen in practice - PermissionAction.CategoryId
        // is non-nullable - but mapped defensively since this crosses two more joins than the rest
        // of this DTO).
        public string? Category { get; set; }

        public string? EmployeeName { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset? ClosedAtUtc { get; set; }

        public bool IsFalsePositive { get; set; }
    }
}
