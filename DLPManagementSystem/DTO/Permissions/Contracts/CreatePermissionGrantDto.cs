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

        // Only meaningful when ActionKey is "file.decrypt" - scopes the grant to files classified at
        // or below this tier (see CompanyDlp.Contracts.ClassificationTiers on the agent side). Null
        // means an ordinary, ungated grant. Validated against DTO.AgentFiles.ClassificationTiers.Order
        // and rejected for any other action key in PermissionGrantService.CreateDirectGrantAsync.
        [StringLength(20)]
        public string? ClassificationTier { get; set; }
    }
}
