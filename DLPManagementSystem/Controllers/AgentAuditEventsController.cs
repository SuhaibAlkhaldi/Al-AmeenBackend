using DLPManagementSystem.Authentication;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentAuditEvents;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [Route("api/v1/agent/events/batch")]
    [ApiController]
    [Authorize(AuthenticationSchemes = DeviceBearerDefaults.SchemeName)]
    public class AgentAuditEventsController : ControllerBase
    {
        private readonly IAgentAuditEventService _agentAuditEventService;

        public AgentAuditEventsController(IAgentAuditEventService agentAuditEventService)
        {
            _agentAuditEventService = agentAuditEventService;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveAuditEventBatch(
            [FromBody] AgentAuditBatchRequestDto request,
            CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            var response = await _agentAuditEventService.ReceiveAuditEventBatchAsync(
                organizationId,
                deviceId,
                request,
                cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response.Data);
        }
    }
}
