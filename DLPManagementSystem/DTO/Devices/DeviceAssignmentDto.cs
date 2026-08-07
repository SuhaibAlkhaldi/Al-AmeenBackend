namespace DLPManagementSystem.DTO.Devices
{
    public sealed class DeviceAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTimeOffset AssignedAtUtc { get; set; }
    }
}
