using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class RevokePermissionGrantDto
    {
        [Required]
        [StringLength(1000)]
        public string RevocationReason { get; set; } = string.Empty;
    }
}
