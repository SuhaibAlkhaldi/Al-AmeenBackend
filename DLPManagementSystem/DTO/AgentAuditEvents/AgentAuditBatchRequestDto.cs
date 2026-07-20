namespace DLPManagementSystem.DTO.AgentAuditEvents
{
    public class AgentAuditBatchRequestDto
    {
        public Guid TenantId { get; set; }

        public Guid DeviceId { get; set; }

        public string AgentVersion { get; set; } = string.Empty;

        public List<SecurityEventEnvelopeDto> Events { get; set; } = new();
    }
}
