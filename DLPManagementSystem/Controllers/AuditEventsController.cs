using DLPManagementSystem.Common;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/audit-events")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk,Auditor")]
    public class AuditEventsController : ControllerBase
    {
        private readonly IAuditEventService _auditEventService;

        public AuditEventsController(IAuditEventService auditEventService)
        {
            _auditEventService = auditEventService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditEvents(
            [FromQuery] Guid? deviceId,
            [FromQuery] Guid? employeeId,
            [FromQuery] string? actionKey,
            [FromQuery] int? decisionId,
            [FromQuery] int? reasonCodeId,
            [FromQuery] DateTimeOffset? fromUtc,
            [FromQuery] DateTimeOffset? toUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var organizationId = User.GetOrganizationId();
            var response = await _auditEventService.GetAuditEventsAsync(
                organizationId, deviceId, employeeId, actionKey, decisionId, reasonCodeId, fromUtc, toUtc, page, pageSize, cancellationToken);
            return Ok(response);
        }
    }
}
