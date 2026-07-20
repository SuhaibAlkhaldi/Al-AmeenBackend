namespace DLPManagementSystem.DTO.Devices
{
    public sealed class DeviceDetailDto : DeviceListItemDto
    {
        public string DeviceKey { get; set; } = string.Empty;

        public string? OsVersion { get; set; }

        public string? SerialNumber { get; set; }

        public string? MacAddress { get; set; }

        public string? AgentVersion { get; set; }

        public Guid? AssignedEmployeeId { get; set; }

        public DateTimeOffset? EnrolledAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
