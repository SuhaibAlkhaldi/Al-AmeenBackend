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
    }
}
