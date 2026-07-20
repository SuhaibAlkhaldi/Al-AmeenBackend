namespace DLPManagementSystem.DTO.Devices
{
    public class DeviceListItemDto
    {
        public Guid Id { get; set; }

        public string MachineName { get; set; } = string.Empty;

        public string? OperatingSystem { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public DateTimeOffset? LastSeenAtUtc { get; set; }

        public long CurrentPolicyVersion { get; set; }

        public string? AssignedEmployeeName { get; set; }
    }
}
