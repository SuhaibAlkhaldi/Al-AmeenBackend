using DLPManagementSystem.Authorization;
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
    //
    // DemoRequests are marketing-landing-page leads owned by the platform itself, not by any tenant,
    // so being SuperAdmin of SOME organization isn't enough: GetList/UpdateStatus below additionally
    // require the "PlatformOwner" policy, which only the platform-owner organization's SuperAdmin
    // satisfies (see PlatformOwnerAuthorizationHandler). A class-level and an action-level [Authorize]
    // are ANDed together (combined into one set of requirements that must ALL succeed), not one
    // replacing the other, so stacking the policy on top of this attribute can only narrow access,
    // never widen it.
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
        [Authorize(Policy = PlatformOwnerPolicy.Name)]
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

        // Class-level and action-level [Authorize] attributes are ANDed together (every requirement
        // from both must succeed), not one replacing the other - so there's no old
        // "SuperAdmin,SecurityAdmin" role override to worry about re-widening this endpoint, and the
        // PlatformOwner policy below only narrows the controller's SuperAdmin-only requirement
        // further, it can't loosen it.
        [HttpPut("{id:guid}/status")]
        [Authorize(Policy = PlatformOwnerPolicy.Name)]
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
