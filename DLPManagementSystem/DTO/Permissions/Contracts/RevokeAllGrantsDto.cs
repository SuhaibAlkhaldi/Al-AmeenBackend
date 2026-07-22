using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class RevokeAllGrantsDto
    {
        [Required]
        [StringLength(250)]
        public string SubjectId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string RevocationReason { get; set; } = string.Empty;
    }
}
