using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Employees
{
    public sealed class UpdateEmployeeDto
    {
        public Guid? DepartmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        [Required]
        public int StatusId { get; set; }
    }
}
