using System.ComponentModel.DataAnnotations;
using DLPManagementSystem.Common;

namespace DLPManagementSystem.DTO.Auth
{
    public sealed class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        // Defense in depth - AuthService.ChangePasswordAsync performs the same check explicitly and
        // is what actually produces the message a caller sees; see PasswordPolicy for why.
        [Required]
        [MinLength(PasswordPolicy.MinLength)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
