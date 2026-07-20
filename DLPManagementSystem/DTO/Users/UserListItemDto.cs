namespace DLPManagementSystem.DTO.Users
{
    public class UserListItemDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public DateTimeOffset? LastLoginAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
