using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Alerts
{
    public sealed class UpdateAlertStatusDto
    {
        [Required]
        public int AlertStatusId { get; set; }

        [StringLength(2000)]
        public string? InvestigationNotes { get; set; }

        public bool? IsFalsePositive { get; set; }
    }
}
