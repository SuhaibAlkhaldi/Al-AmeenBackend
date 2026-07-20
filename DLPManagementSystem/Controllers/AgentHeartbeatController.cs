using DLPManagementSystem.DTO.AgentHeartbeat;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/agent/heartbeat")]
    public class AgentHeartbeatController : ControllerBase
    {
        private readonly IAgentHeartbeatService _service;

        public AgentHeartbeatController(IAgentHeartbeatService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveHeartbeat(
            [FromBody] AgentHeartbeatRequestDto? request,
            CancellationToken cancellationToken)
        {
            var deviceKey = Request.Headers["X-Device-Key"].FirstOrDefault();
            var agentSecret = Request.Headers["X-Agent-Secret"].FirstOrDefault();
            var agentVersion = Request.Headers["X-CompanyDlp-AgentVersion"].FirstOrDefault();

            var response = await _service.ReceiveHeartbeatAsync(
                deviceKey ?? string.Empty,
                agentSecret ?? string.Empty,
                agentVersion,
                request,
                cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}

