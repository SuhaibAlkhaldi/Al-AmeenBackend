namespace DLPManagementSystem.DTO.Users
{
    public sealed class UserDetailDto : UserListItemDto
    {
        public Guid? EmployeeId { get; set; }

        public string? EmployeeName { get; set; }
    }
}
