namespace DLPManagementSystem.DTO.Employees
{
    public sealed class EmployeeWindowsIdentityDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? DomainName { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserSid { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? RevokedAtUtc { get; set; }
    }
}
