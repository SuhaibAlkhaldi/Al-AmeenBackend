using System.ComponentModel.DataAnnotations;

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

        [Required]
        [MinLength(10)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int UserTypeId { get; set; }
    }
}
