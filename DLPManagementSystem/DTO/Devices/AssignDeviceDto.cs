using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Devices
{
    public sealed class AssignDeviceDto
    {
        [Required]
        public Guid EmployeeId { get; set; }
    }
}
