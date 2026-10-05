using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileTracking;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    // The diff shows the text of sensitive files, so it is narrower than the file inventory reports: only the roles that
    // already see the content of sensitive material (SuperAdmin, SecurityAdmin). HelpDesk and Auditor get 403 here.
    [ApiController]
    [Route("api/v1/file-version-diff")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
    public class FileVersionDiffController : ControllerBase
    {
        private readonly IFileVersionDiffService _fileVersionDiffService;

        public FileVersionDiffController(IFileVersionDiffService fileVersionDiffService)
        {
            _fileVersionDiffService = fileVersionDiffService;
        }

        [HttpGet("{versionId:guid}")]
        public async Task<ActionResult<ApiResponse<FileVersionDiffDto>>> GetDiff(Guid versionId, CancellationToken cancellationToken = default)
        {
            var response = await _fileVersionDiffService.GetDiffAsync(
                User.GetOrganizationId(), User.GetUserId(), versionId, cancellationToken);

            if (response.Success is false)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
