using DLPManagementSystem.Authentication;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentHeartbeat;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/agent/heartbeat")]
    [Authorize(AuthenticationSchemes = DeviceBearerDefaults.SchemeName)]
    public class AgentHeartbeatController : ControllerBase
    {
        private readonly IAgentHeartbeatService _service;

        public AgentHeartbeatController(IAgentHeartbeatService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveHeartbeat(
            [FromBody] AgentHeartbeatRequestDto request,
            CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            var response = await _service.ReceiveHeartbeatAsync(organizationId, deviceId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response.Data);
        }
    }
}
