namespace DLPManagementSystem.DTO.Employees
{
    public class EmployeeListItemDto
    {
        public Guid Id { get; set; }

        public Guid? DepartmentId { get; set; }

        public string? DepartmentName { get; set; }

        public string EmployeeNumber { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
