using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.DemoRequests
{
    public class UpdateDemoRequestStatusDto
    {
        [Required]
        public int StatusId { get; set; }
    }
}
