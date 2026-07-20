using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Users
{
    public sealed class UpdateUserDto
    {
        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int StatusId { get; set; }
    }
}
