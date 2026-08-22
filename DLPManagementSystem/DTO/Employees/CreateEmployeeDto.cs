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

        // Required on this create-only path (an employee must always have a device), but deliberately
        // NOT annotated [Required] here - ASP.NET Core's model validation runs before the controller
        // action, so a [Required] failure here would short-circuit straight to Program.cs's generic
        // "Please check the fields marked below." fallback (only Password/NewPassword/CurrentPassword
        // are special-cased there) and EmployeeService.CreateEmployeeAsync's specific, clearer bilingual
        // message would never be reached for the (most common) omitted-field case - only for an
        // explicit Guid.Empty. The service's own check already covers null and Guid.Empty identically,
        // so this is deliberately left to the one real check rather than a [Required] duplicate that
        // would just produce a worse message some of the time. UpdateEmployeeDto is a separate type
        // entirely, so existing employees are never retroactively affected by this.
        public Guid? DeviceId { get; set; }
    }
}
