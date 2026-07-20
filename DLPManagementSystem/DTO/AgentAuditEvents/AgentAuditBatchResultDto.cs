namespace DLPManagementSystem.DTO.AgentAuditEvents
{
    public class AgentAuditBatchResultDto
    {
        public Guid BatchId { get; set; }
        public int ReceivedEvents { get; set; }
        public bool AlreadyProcessed { get; set; }
    }
}
