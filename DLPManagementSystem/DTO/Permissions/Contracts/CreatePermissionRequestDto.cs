using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class CreatePermissionRequestDto
    {
        [Required]
        [StringLength(100)]
        public string ActionKey { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string GrantType { get; set; } = string.Empty;
        public DateTimeOffset? RequestedStartsAtUtc { get; set; }

        public DateTimeOffset? RequestedExpiresAtUtc { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 5)]
        public string BusinessJustification { get; set; } = string.Empty;
    }
}
