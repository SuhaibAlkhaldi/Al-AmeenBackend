using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Employees
{
    // Manual admin entry only, by design (see AddWindowsIdentityAsync) - no auto-detection/self-
    // registration path exists in this phase.
    public sealed class CreateEmployeeWindowsIdentityDto
    {
        public string? DomainName { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string UserSid { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }
    }
}
