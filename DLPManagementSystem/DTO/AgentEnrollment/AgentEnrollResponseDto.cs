namespace DLPManagementSystem.DTO.AgentEnrollment
{
    public class AgentEnrollResponseDto
    {
        public string DeviceKey { get; set; } = string.Empty;
        public string AgentSecret { get; set; } = string.Empty;
        public DateTime EnrolledAtUtc { get; set; }
    }
}
