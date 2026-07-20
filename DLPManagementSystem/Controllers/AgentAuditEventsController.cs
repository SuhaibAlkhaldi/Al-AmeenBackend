using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentAuditEvents;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [Route("api/agent/audit-events")]
    [ApiController]
    public class AgentAuditEventsController : ControllerBase
    {
        private readonly IAgentAuditEventService _agentAuditEventService;

        public AgentAuditEventsController(IAgentAuditEventService agentAuditEventService)
        {
            _agentAuditEventService = agentAuditEventService;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveAuditEvents(
            [FromHeader(Name = "X-Device-Key")] string deviceKey,
            [FromHeader(Name = "X-Agent-Secret")] string agentSecret,
            [FromBody] AgentAuditBatchRequestDto request,
            CancellationToken cancellationToken)
        {
            var response = await _agentAuditEventService.ReceiveAuditEvents(
                deviceKey,
                agentSecret,
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
