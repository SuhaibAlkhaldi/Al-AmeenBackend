using System.ComponentModel.DataAnnotations;
using DLPManagementSystem.Common;

namespace DLPManagementSystem.DTO.Users
{
    public sealed class ResetPasswordDto
    {
        // Defense in depth - UserService.ResetPasswordAsync performs the same check explicitly and
        // is what actually produces the message a caller sees; see PasswordPolicy for why.
        [Required]
        [MinLength(PasswordPolicy.MinLength)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
