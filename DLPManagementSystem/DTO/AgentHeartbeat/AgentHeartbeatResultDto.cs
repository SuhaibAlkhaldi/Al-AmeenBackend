namespace DLPManagementSystem.DTO.AgentHeartbeat
{
    public class AgentHeartbeatResultDto
    {
        public DateTimeOffset ServerTimeUtc { get; set; }
        public bool PolicyRefreshRequired { get; set; }
    }
}
