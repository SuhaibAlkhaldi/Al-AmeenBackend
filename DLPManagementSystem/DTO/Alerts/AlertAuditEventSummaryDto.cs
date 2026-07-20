namespace DLPManagementSystem.DTO.Alerts
{
    public sealed class AlertAuditEventSummaryDto
    {
        public Guid AuditEventId { get; set; }

        public string DeviceName { get; set; } = string.Empty;

        public string? EmployeeName { get; set; }

        public string ActionKey { get; set; } = string.Empty;

        public DateTimeOffset OccurredAtUtc { get; set; }

        public string? AiDecision { get; set; }

        public decimal? AiRiskScore { get; set; }
    }
}
