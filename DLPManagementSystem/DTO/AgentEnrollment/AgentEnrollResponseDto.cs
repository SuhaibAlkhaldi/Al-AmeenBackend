namespace DLPManagementSystem.DTO.AgentEnrollment
{
    public class AgentEnrollResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; set; }
    }
}
