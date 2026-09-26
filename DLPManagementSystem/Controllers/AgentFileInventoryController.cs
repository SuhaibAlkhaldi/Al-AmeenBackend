using DLPManagementSystem.Authentication;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFileInventory;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [Route("api/v1/agent/file-inventory/batch")]
    [ApiController]
    [Authorize(AuthenticationSchemes = DeviceBearerDefaults.SchemeName)]
    public class AgentFileInventoryController : ControllerBase
    {
        private readonly IAgentFileInventoryService _agentFileInventoryService;

        public AgentFileInventoryController(IAgentFileInventoryService agentFileInventoryService)
        {
            _agentFileInventoryService = agentFileInventoryService;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveFileInventoryBatch(
            [FromBody] AgentFileInventoryBatchRequestDto request,
            CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            var response = await _agentFileInventoryService.ApplyFileInventoryBatchAsync(
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
