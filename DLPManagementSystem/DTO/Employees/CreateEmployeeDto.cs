using System.ComponentModel.DataAnnotations;
using DLPManagementSystem.Common;

namespace DLPManagementSystem.DTO.Employees
{
    public sealed class CreateEmployeeDto
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

        // Optional - when omitted, the linked login account gets a hidden, unguessable password (as
        // before) until an admin resets it. When given, EmployeeService.CreateEmployeeAsync performs
        // the same length check explicitly (see PasswordPolicy); this annotation is defense in depth.
        [MinLength(PasswordPolicy.MinLength)]
        public string? Password { get; set; }
    }
}
