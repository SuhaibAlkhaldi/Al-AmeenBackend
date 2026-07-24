using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Users
{
    public sealed class ResetPasswordDto
    {
        [Required]
        [MinLength(10)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
