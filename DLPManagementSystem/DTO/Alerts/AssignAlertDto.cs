using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Alerts
{
    public sealed class AssignAlertDto
    {
        [Required]
        public Guid AssignedToUserId { get; set; }
    }
}
