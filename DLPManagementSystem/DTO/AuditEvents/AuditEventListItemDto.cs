namespace DLPManagementSystem.DTO.AuditEvents
{
    public class AuditEventListItemDto
    {
        public Guid Id { get; set; }

        public DateTimeOffset OccurredAtUtc { get; set; }

        public DateTimeOffset ReceivedAtUtc { get; set; }

        public Guid DeviceId { get; set; }

        public string DeviceName { get; set; } = string.Empty;

        public Guid? EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public string? Username { get; set; }

        public string ActionKey { get; set; } = string.Empty;

        public int DecisionId { get; set; }

        public string DecisionName { get; set; } = string.Empty;

        public string DecisionDisplayName { get; set; } = string.Empty;

        public int? ReasonCodeId { get; set; }

        public string? ReasonCodeDisplayName { get; set; }

        public long? PolicyVersion { get; set; }

        public string? Details { get; set; }
    }
}
