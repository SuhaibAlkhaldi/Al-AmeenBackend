namespace DLPManagementSystem.DTO.AgentHeartbeat
{
    public class AgentHeartbeatRequestDto
    {
        public Guid TenantId { get; set; }
        public Guid DeviceId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string AgentVersion { get; set; } = string.Empty;
        public string? OsVersion { get; set; }
        public string? OperatingSystemEdition { get; set; }
        public string? SerialNumber { get; set; }
        public string? MacAddress { get; set; }
        public DateTimeOffset SentAtUtc { get; set; }
        public long LastAppliedPolicyVersion { get; set; }
        public int PendingAuditEventCount { get; set; }
    }
}
