namespace DLPManagementSystem.DTO.AgentHeartbeat
{
    public class AgentHeartbeatResultDto
    {
        public Guid DeviceId { get; set; }
        public DateTimeOffset LastSeenAtUtc { get; set; }
        public string? AgentVersion { get; set; }
        public long CurrentPolicyVersion { get; set; }
    }
}
