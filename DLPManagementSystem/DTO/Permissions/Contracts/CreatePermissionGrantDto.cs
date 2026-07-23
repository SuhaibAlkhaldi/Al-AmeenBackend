using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class CreatePermissionGrantDto
    {
        [Required]
        public string SubjectId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ActionKey { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string GrantType { get; set; } = string.Empty;

        public DateTimeOffset? StartsAtUtc { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }

        [Required]
        [StringLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }
}
