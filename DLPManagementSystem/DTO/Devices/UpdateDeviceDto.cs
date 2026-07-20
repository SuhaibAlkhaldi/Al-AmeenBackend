using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.Devices
{
    public sealed class UpdateDeviceDto
    {
        [Required]
        [StringLength(150)]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        public int StatusId { get; set; }
    }
}
