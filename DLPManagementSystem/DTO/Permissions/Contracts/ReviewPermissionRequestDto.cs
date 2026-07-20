using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class ReviewPermissionRequestDto
    {
        [StringLength(2000)]
        public string? ReviewNotes { get; set; }
    }
}
