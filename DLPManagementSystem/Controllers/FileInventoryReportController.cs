using DLPManagementSystem.Common;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/file-inventory-report")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk,Auditor")]
    public class FileInventoryReportController : ControllerBase
    {
        private readonly IFileInventoryReportService _fileInventoryReportService;

        public FileInventoryReportController(IFileInventoryReportService fileInventoryReportService)
        {
            _fileInventoryReportService = fileInventoryReportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFileInventory(
            [FromQuery] List<Guid>? employeeIds,
            [FromQuery] List<string>? classifications,
            [FromQuery] List<string>? provenances,
            [FromQuery] List<int>? fileTypeIds,
            [FromQuery] bool? isProtected,
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var organizationId = User.GetOrganizationId();
            var response = await _fileInventoryReportService.GetFileInventoryAsync(
                organizationId, employeeIds, classifications, provenances, fileTypeIds, isProtected, fromUtc, toUtc, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task ExportFileInventory(
            [FromQuery] List<Guid>? employeeIds,
            [FromQuery] List<string>? classifications,
            [FromQuery] List<string>? provenances,
            [FromQuery] List<int>? fileTypeIds,
            [FromQuery] bool? isProtected,
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtc,
            CancellationToken cancellationToken = default)
        {
            var organizationId = User.GetOrganizationId();
            await _fileInventoryReportService.ExportFileInventoryAsync(
                organizationId, employeeIds, classifications, provenances, fileTypeIds, isProtected, fromUtc, toUtc, Response, cancellationToken);
        }
    }
}
