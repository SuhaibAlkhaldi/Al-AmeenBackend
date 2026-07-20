namespace DLPManagementSystem.DTO.AgentEnrollment
{
    public class AgentEnrollRequestDto
    {
        public Guid TenantId { get; set; }
        public Guid DeviceId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string AgentVersion { get; set; } = string.Empty;
        public string EnrollmentCode { get; set; } = string.Empty;
    }
}
