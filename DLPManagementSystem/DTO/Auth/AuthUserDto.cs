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

        // True only for the platform-owner organization's users (Code "UII", or "DEV" while running
        // in Development) - see PlatformOwnerPolicy.IsOwnerOrganizationCode. The frontend uses this
        // to hide/guard platform-owner-only UI (e.g. the Demo Requests nav item and route); the
        // actual enforcement is still DemoRequestsController's "PlatformOwner" authorization policy.
        public bool IsPlatformOwner { get; set; }
    }
}
