namespace DLPManagementSystem.DTO.Alerts
{
    public sealed class AlertDetailDto : AlertListItemDto
    {
        public string? Description { get; set; }

        public string? InvestigationNotes { get; set; }

        public AlertAuditEventSummaryDto AuditEvent { get; set; } = null!;
    }
}
