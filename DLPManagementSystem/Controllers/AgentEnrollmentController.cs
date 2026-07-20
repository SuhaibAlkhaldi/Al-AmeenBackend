using DLPManagementSystem.DTO.AgentEnrollment;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [Route("api/agent/enroll")]
    [ApiController]
    public class AgentEnrollmentController : ControllerBase
    {
        private readonly IAgentEnrollmentService _agentEnrollmentService;

        public AgentEnrollmentController(IAgentEnrollmentService agentEnrollmentService)
        {
            _agentEnrollmentService = agentEnrollmentService;
        }

        [HttpPost]
        public async Task<IActionResult> Enroll([FromBody] AgentEnrollRequestDto request,CancellationToken cancellationToken)
        {
            var response = await _agentEnrollmentService.Enroll(request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
