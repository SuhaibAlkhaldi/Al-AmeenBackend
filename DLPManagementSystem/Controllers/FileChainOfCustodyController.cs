using DLPManagementSystem.Common;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/file-chain-of-custody")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk,Auditor")]
    public class FileChainOfCustodyController : ControllerBase
    {
        private readonly IFileChainOfCustodyService _fileChainOfCustodyService;
        private readonly IFileVersionHistoryService _fileVersionHistoryService;

        public FileChainOfCustodyController(
            IFileChainOfCustodyService fileChainOfCustodyService,
            IFileVersionHistoryService fileVersionHistoryService)
        {
            _fileChainOfCustodyService = fileChainOfCustodyService;
            _fileVersionHistoryService = fileVersionHistoryService;
        }

        [HttpGet("{fileId:guid}")]
        public async Task<IActionResult> GetChainOfCustody(Guid fileId, CancellationToken cancellationToken = default)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _fileChainOfCustodyService.GetChainOfCustodyAsync(organizationId, fileId, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{fileId:guid}/version-history")]
        public async Task<IActionResult> GetVersionHistory(Guid fileId, CancellationToken cancellationToken = default)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _fileVersionHistoryService.GetHistoryAsync(organizationId, fileId, cancellationToken);
            return Ok(response);
        }
    }
}
