namespace DLPManagementSystem.DTO.AgentAuditEvents
{
    public class RejectedAuditEventDto
    {
        public Guid EventId { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public bool Retryable { get; set; }
    }

    public class AgentAuditBatchResultDto
    {
        public List<Guid> AcceptedEventIds { get; set; } = new();
        public List<Guid> DuplicateEventIds { get; set; } = new();
        public List<RejectedAuditEventDto> RejectedEvents { get; set; } = new();
    }
}
