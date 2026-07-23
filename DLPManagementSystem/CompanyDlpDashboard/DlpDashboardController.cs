using DLPManagementSystem.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.CompanyDlpDashboard;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DlpDashboardController : ControllerBase
{
    private readonly IDlpDashboardQueryService _dashboardQueryService;

    public DlpDashboardController(IDlpDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DlpDashboardSummaryDto>> GetSummary(
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var organizationId = User.GetOrganizationId();
        var summary = await _dashboardQueryService.GetSummaryAsync(organizationId, fromUtc, toUtc, cancellationToken);
        return Ok(summary);
    }
}
