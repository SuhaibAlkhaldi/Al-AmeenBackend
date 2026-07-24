using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Auth
{
    public sealed class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(10)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
