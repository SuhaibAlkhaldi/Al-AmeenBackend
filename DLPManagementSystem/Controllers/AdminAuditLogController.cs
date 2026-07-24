using DLPManagementSystem.Common;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/admin-audit-log")]
    // HelpDesk is included even though HelpDesk accounts can also be a *subject* of this log (they can
    // create/update users and employees): hiding their own actions from them would undermine the
    // accountability this log exists for, and HelpDesk already has equivalent read access to the
    // employee/agent AuditEvents log (AuditEventsController). SecurityAdmin/Auditor act as the
    // independent oversight layer over HelpDesk's actions the same way they already do for AuditEvents.
    [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk,Auditor")]
    public class AdminAuditLogController : ControllerBase
    {
        private readonly IAdminAuditLogService _adminAuditLogService;

        public AdminAuditLogController(IAdminAuditLogService adminAuditLogService)
        {
            _adminAuditLogService = adminAuditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAdminAuditLog(
            [FromQuery] Guid? actorUserId,
            [FromQuery] string? actionType,
            [FromQuery] string? targetType,
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var organizationId = User.GetOrganizationId();
            var response = await _adminAuditLogService.GetAdminAuditLogsAsync(
                organizationId, actorUserId, actionType, targetType, fromUtc, toUtc, page, pageSize, cancellationToken);
            return Ok(response);
        }
    }
}
