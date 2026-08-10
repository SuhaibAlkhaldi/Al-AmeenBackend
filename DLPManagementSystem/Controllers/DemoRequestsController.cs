using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.DemoRequests;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/demo-requests")]
    // SuperAdmin only — sales-lead data, not part of any other role's job function. Deliberately
    // narrower than most other admin list endpoints (Alerts, Audit Events, etc), which are open to
    // SecurityAdmin/HelpDesk/Auditor too.
    [Authorize(Roles = "SuperAdmin")]
    public class DemoRequestsController : ControllerBase
    {
        private readonly IDemoRequestService _demoRequestService;

        public DemoRequestsController(IDemoRequestService demoRequestService)
        {
            _demoRequestService = demoRequestService;
        }

        // Public: submitted by anonymous visitors from the marketing landing page, which has no
        // login of its own. Rate-limited below since it's an unauthenticated write endpoint.
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimiterPolicies.DemoRequests)]
        public async Task<IActionResult> Create([FromBody] CreateDemoRequestDto request, CancellationToken cancellationToken)
        {
            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _demoRequestService.CreateAsync(request, sourceIp, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] int? statusId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var response = await _demoRequestService.GetListAsync(statusId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        // No action-level [Authorize] override here — inherits the controller's SuperAdmin-only
        // policy. An action-level [Authorize(Roles=...)] REPLACES the controller-level one rather
        // than intersecting with it, so leaving the old "SuperAdmin,SecurityAdmin" override in
        // place would have silently re-widened this one endpoint back open to SecurityAdmin.
        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDemoRequestStatusDto request, CancellationToken cancellationToken)
        {
            var response = await _demoRequestService.UpdateStatusAsync(id, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
