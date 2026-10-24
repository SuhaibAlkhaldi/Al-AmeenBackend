using DLPManagementSystem.Authentication;
using DLPManagementSystem.Common;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/agent/unenroll")]
    [Authorize(AuthenticationSchemes = DeviceBearerDefaults.SchemeName)]
    public class AgentUnenrollController : ControllerBase
    {
        private readonly IAgentEnrollmentService _agentEnrollmentService;

        public AgentUnenrollController(IAgentEnrollmentService agentEnrollmentService)
        {
            _agentEnrollmentService = agentEnrollmentService;
        }

        [HttpPost]
        public async Task<IActionResult> Unenroll(CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            var response = await _agentEnrollmentService.Unenroll(organizationId, deviceId, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response.Data);
        }
    }
}
