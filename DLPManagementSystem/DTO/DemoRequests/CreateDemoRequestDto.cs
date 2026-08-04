using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.DemoRequests
{
    public class CreateDemoRequestDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string CompanyEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string CompanySize { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }
    }
}
