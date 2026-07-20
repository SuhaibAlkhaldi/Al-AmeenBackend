using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.EnrollmentTokens
{
    public sealed class CreateEnrollmentTokenDto
    {
        [Required]
        [StringLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        public int? ExpiresInDays { get; set; }

        public int? MaxUses { get; set; }
    }
}
