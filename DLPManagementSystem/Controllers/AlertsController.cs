using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Alerts;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/alerts")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk,Auditor")]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertsController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAlerts(
            [FromQuery] int? statusId,
            [FromQuery] int? levelId,
            [FromQuery] Guid? assignedToUserId,
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var organizationId = User.GetOrganizationId();
            var response = await _alertService.GetAlertsAsync(organizationId, statusId, levelId, assignedToUserId, fromUtc, toUtc, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("export")]
        public async Task ExportAlerts(
            [FromQuery] int? statusId,
            [FromQuery] int? levelId,
            [FromQuery] Guid? assignedToUserId,
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtc,
            CancellationToken cancellationToken = default)
        {
            var organizationId = User.GetOrganizationId();
            await _alertService.ExportAlertsAsync(organizationId, statusId, levelId, assignedToUserId, fromUtc, toUtc, Response, cancellationToken);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAlertById(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _alertService.GetAlertByIdAsync(organizationId, id, cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}/assign")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
        public async Task<IActionResult> AssignAlert(Guid id, [FromBody] AssignAlertDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _alertService.AssignAlertAsync(organizationId, id, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
        public async Task<IActionResult> UpdateAlertStatus(Guid id, [FromBody] UpdateAlertStatusDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _alertService.UpdateAlertStatusAsync(organizationId, id, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
