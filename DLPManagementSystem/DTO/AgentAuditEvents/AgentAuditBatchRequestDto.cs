namespace DLPManagementSystem.DTO.AgentAuditEvents
{
    public class AgentAuditBatchRequestDto
    {
        public string DeviceKey { get; set; } = string.Empty;

        public string AgentVersion { get; set; } = string.Empty;

        public long PolicyVersion { get; set; }

        public Guid BatchId { get; set; }

        public List<AgentAuditEventRequestDto> Events { get; set; } = new();
    }
}
