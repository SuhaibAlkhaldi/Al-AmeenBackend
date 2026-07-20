namespace DLPManagementSystem.DTO.Employees
{
    public sealed class EmployeeDetailDto : EmployeeListItemDto
    {
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }
}
