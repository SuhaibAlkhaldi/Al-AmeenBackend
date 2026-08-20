namespace DLPManagementSystem.DTO.Auth
{
    public sealed class AuthUserDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public Guid OrganizationId { get; set; }

        public int UserTypeId { get; set; }

        // When true, the frontend forces a redirect to the change-password page and blocks every
        // other route until it's cleared - see User.MustChangePassword.
        public bool MustChangePassword { get; set; }
    }
}
