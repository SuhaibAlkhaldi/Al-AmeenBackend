using DLPManagementSystem.Authentication;
using DLPManagementSystem.Common;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/agent/policy")]
    [Authorize(AuthenticationSchemes = DeviceBearerDefaults.SchemeName)]
    public class AgentPolicyController : ControllerBase
    {
        private readonly IAgentPolicyService _service;

        public AgentPolicyController(IAgentPolicyService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPolicy(
            [FromQuery] Guid tenantId,
            [FromQuery] Guid deviceId,
            [FromQuery] long currentVersion,
            [FromQuery] string? userSid,
            CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var authenticatedDeviceId = User.GetDeviceId();

            if (tenantId != organizationId || deviceId != authenticatedDeviceId)
            {
                return BadRequest("tenantId/deviceId do not match the authenticated device.");
            }

            var response = await _service.GetPolicyAsync(
                organizationId,
                authenticatedDeviceId,
                currentVersion,
                userSid,
                cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            if (response.Data?.HasUpdate != true || response.Data.Snapshot == null)
            {
                return NoContent();
            }

            return Ok(response.Data.Snapshot);
        }
    }
}
