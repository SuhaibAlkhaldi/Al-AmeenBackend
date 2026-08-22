using System.ComponentModel.DataAnnotations;
using DLPManagementSystem.Common;

namespace DLPManagementSystem.DTO.Users
{
    public sealed class CreateUserDto
    {
        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        // Defense in depth - UserService.CreateUserAsync performs the same check explicitly and is
        // what actually produces the message a caller sees; see PasswordPolicy for why.
        [Required]
        [MinLength(PasswordPolicy.MinLength)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int UserTypeId { get; set; }

        // Optional, unlike CreateEmployeeDto.DeviceId - an Admin-type account is never required to
        // own a device. When present, UserService.CreateUserAsync validates it (exists, same
        // organization, Active) and links it exactly like the Employee create path does.
        public Guid? DeviceId { get; set; }

        // Optional. Only meaningful when a role whose job function the UI suggests defaults for
        // (SecurityAdmin/SuperAdmin today) is selected together with DeviceId - each key here becomes
        // its own permanent PermissionGrant for the account's linked Employee record, created as part
        // of this same request. Never applied silently: the frontend only ever sends keys an admin
        // explicitly checked, all unchecked by default. Any enabled PermissionAction key is accepted;
        // the specific "suggested" set per role is a frontend curation concern, not a backend
        // restriction - the admin already has unrestricted direct-grant access via the Access page.
        public List<string>? SuggestedPermissionActionKeys { get; set; }
    }
}
