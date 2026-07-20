namespace DLPManagementSystem.DTO.AgentHeartbeat
{
    public class AgentHeartbeatRequestDto
    {
        public long? PolicyVersion { get; set; }

        public long? CurrentPolicyVersion { get; set; }

        public string? PolicyHash { get; set; }

        public string? Status { get; set; }
    }
}
