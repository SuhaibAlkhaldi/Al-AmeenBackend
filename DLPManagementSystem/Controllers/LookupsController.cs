using DLPManagementSystem.Common;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/lookups")]
    [Authorize]
    public class LookupsController : ControllerBase
    {
        private readonly ILookupsService _lookupsService;

        public LookupsController(ILookupsService lookupsService)
        {
            _lookupsService = lookupsService;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            var data = await _lookupsService.GetRolesAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }

        [HttpGet("alert-levels")]
        public async Task<IActionResult> GetAlertLevels(CancellationToken cancellationToken)
        {
            var data = await _lookupsService.GetAlertLevelsAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }

        [HttpGet("alert-statuses")]
        public async Task<IActionResult> GetAlertStatuses(CancellationToken cancellationToken)
        {
            var data = await _lookupsService.GetAlertStatusesAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }

        [HttpGet("device-statuses")]
        public async Task<IActionResult> GetDeviceStatuses(CancellationToken cancellationToken)
        {
            var data = await _lookupsService.GetDeviceStatusesAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }

        [HttpGet("employee-statuses")]
        public async Task<IActionResult> GetEmployeeStatuses(CancellationToken cancellationToken)
        {
            var data = await _lookupsService.GetEmployeeStatusesAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }

        [HttpGet("user-statuses")]
        public async Task<IActionResult> GetUserStatuses(CancellationToken cancellationToken)
        {
            var data = await _lookupsService.GetUserStatusesAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var data = await _lookupsService.GetDepartmentsAsync(organizationId, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }

        [HttpGet("/api/v1/permission-actions")]
        public async Task<IActionResult> GetPermissionActions(CancellationToken cancellationToken)
        {
            var data = await _lookupsService.GetPermissionActionsAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }

        [HttpGet("audit-decisions")]
        public async Task<IActionResult> GetAuditDecisions(CancellationToken cancellationToken)
        {
            var data = await _lookupsService.GetAuditDecisionsAsync(cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(data));
        }
    }
}
