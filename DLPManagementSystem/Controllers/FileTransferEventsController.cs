using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileTransfer;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    // Admin read of observed file transfers. Restricted to SuperAdmin and SecurityAdmin: the rows contain recipient
    // addresses, which HelpDesk and Auditor do not need for their roles.
    [ApiController]
    [Route("api/v1/file-transfer-events")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
    public class FileTransferEventsController : ControllerBase
    {
        private readonly IFileTransferEventService _fileTransferEventService;

        public FileTransferEventsController(IFileTransferEventService fileTransferEventService)
        {
            _fileTransferEventService = fileTransferEventService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResultDto<FileTransferEventListItemDto>>>> GetFileTransferEvents(
            [FromQuery] Guid? fileId,
            [FromQuery] Guid? employeeId,
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await _fileTransferEventService.ListAsync(
                User.GetOrganizationId(),
                fileId,
                employeeId,
                fromUtc,
                toUtc,
                page,
                pageSize,
                cancellationToken);

            return Ok(response);
        }
    }
}
