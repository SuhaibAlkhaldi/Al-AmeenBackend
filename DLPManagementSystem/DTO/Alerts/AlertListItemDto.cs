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

        public string? EmployeeName { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset? ClosedAtUtc { get; set; }

        public bool IsFalsePositive { get; set; }
    }
}
