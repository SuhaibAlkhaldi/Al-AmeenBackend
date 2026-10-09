using DLPManagementSystem.Authentication;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileTransfer;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    // Device-facing ingestion for browser send observations (Phase 7). Identity comes from the device token only; the
    // body's TenantId/DeviceId are checked against it inside the service, never trusted on their own.
    [ApiController]
    [Route("api/v1/agent/file-transfer-events")]
    [Authorize(AuthenticationSchemes = DeviceBearerDefaults.SchemeName)]
    public class AgentFileTransferEventsController : ControllerBase
    {
        private readonly IFileTransferEventService _fileTransferEventService;

        public AgentFileTransferEventsController(IFileTransferEventService fileTransferEventService)
        {
            _fileTransferEventService = fileTransferEventService;
        }

        [HttpPost("batch")]
        public async Task<IActionResult> ReceiveFileTransferEvents(
            [FromBody] FileTransferEventBatchRequestDto request,
            CancellationToken cancellationToken)
        {
            var response = await _fileTransferEventService.IngestAsync(
                User.GetOrganizationId(),
                User.GetDeviceId(),
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
