using System.Text.Json;
using DLPManagementSystem.Authentication;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentAuditEvents;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Controllers
{
    [Route("api/v1/agent/events/batch")]
    [ApiController]
    [Authorize(AuthenticationSchemes = DeviceBearerDefaults.SchemeName)]
    public class AgentAuditEventsController : ControllerBase
    {
        private readonly IAgentAuditEventService _agentAuditEventService;
        private readonly IOptions<JsonOptions> _jsonOptions;

        public AgentAuditEventsController(
            IAgentAuditEventService agentAuditEventService,
            IOptions<JsonOptions> jsonOptions)
        {
            _agentAuditEventService = agentAuditEventService;
            _jsonOptions = jsonOptions;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveAuditEventBatch(
            // Bound as the raw JsonElement (rather than the typed DTO directly) so the service can
            // verify each event's integrityHash against the exact bytes the agent sent, not a
            // reconstructed/re-typed copy - see AuditIntegrityVerifier for why that distinction matters.
            // The typed DTO is still deserialized below, from this same JsonElement, using the identical
            // options ASP.NET Core's own [FromBody] binding would have used - existing behavior for
            // every field other than integrityHash is unchanged.
            [FromBody] JsonElement rawBody,
            CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            var request = rawBody.Deserialize<AgentAuditBatchRequestDto>(_jsonOptions.Value.JsonSerializerOptions)
                ?? new AgentAuditBatchRequestDto();

            var response = await _agentAuditEventService.ReceiveAuditEventBatchAsync(
                organizationId,
                deviceId,
                request,
                rawBody,
                cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response.Data);
        }
    }
}
