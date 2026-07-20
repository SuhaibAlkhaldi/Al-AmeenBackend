namespace DLPManagementSystem.DTO.AgentEnrollment
{
    public class AgentEnrollRequestDto
    {
        public string EnrollmentToken { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string? MachineSid { get; set; }
        public string OperatingSystem { get; set; } = string.Empty;
        public string? OsVersion { get; set; }
        public string? SerialNumber { get; set; }
        public string? MacAddress { get; set; }
        public string AgentVersion { get; set; } = string.Empty;
    }
}
