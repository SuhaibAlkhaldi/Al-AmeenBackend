using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/agent/policy")]
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
            CancellationToken cancellationToken)
        {
            var deviceKey = Request.Headers["X-Device-Key"].FirstOrDefault();
            var agentSecret = Request.Headers["X-Agent-Secret"].FirstOrDefault();

            var response = await _service.GetPolicyAsync(
                deviceKey ?? string.Empty,
                agentSecret ?? string.Empty,
                tenantId,
                deviceId,
                currentVersion,
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
