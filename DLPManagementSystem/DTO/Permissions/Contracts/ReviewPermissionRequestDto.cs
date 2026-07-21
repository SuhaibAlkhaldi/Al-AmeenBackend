using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class ReviewPermissionRequestDto
    {
        [StringLength(2000)]
        public string? ReviewNotes { get; set; }

        // Approve-only overrides; null falls back to the employee's originally requested values.
        [StringLength(50)]
        public string? GrantType { get; set; }

        public DateTimeOffset? StartsAtUtc { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }
    }
}
