using System.Diagnostics;
using DLPManagementSystem.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace DLPManagementSystem.CompanyDlpDashboard;

/// <summary>
/// Dashboard data for the signed-in user's organization. <see cref="GetSummary"/> is the original,
/// single heavy endpoint - still served unchanged for any client still on it. The six endpoints
/// below (dashboard-split-endpoints) split that same data into independent, independently-cached
/// pieces so a slow one (e.g. risk scoring) never holds up a fast one (e.g. status).
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DlpDashboardController : ControllerBase
{
    private static readonly TimeSpan KpisCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BreakdownCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RecentEventsCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StatusCacheDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan RiskCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan WeeklyTrendCacheDuration = TimeSpan.FromMinutes(5);

    private readonly IDlpDashboardQueryService _dashboardQueryService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DlpDashboardController> _logger;

    public DlpDashboardController(
        IDlpDashboardQueryService dashboardQueryService,
        IMemoryCache cache,
        ILogger<DlpDashboardController> logger)
    {
        _dashboardQueryService = dashboardQueryService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// The original combined dashboard payload (totals, breakdowns, recent events, risk, weekly
    /// trend, endpoint agents, permission request counts - everything in one call). Kept exactly
    /// as-is for any client still depending on it; new clients should prefer the split endpoints
    /// below instead.
    /// </summary>
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

    /// <summary>
    /// Headline totals (current + previous-period, for a %-change indicator) and a time-bucketed
    /// series for the sparklines.
    /// </summary>
    /// <param name="range">
    /// One of <c>6h</c>, <c>24h</c>, <c>7d</c>. Defaults to <c>24h</c> when omitted. Any other value
    /// returns 400. The window is always computed server-side (now, floored to the minute, minus
    /// the range's duration) - fromUtc/toUtc are not accepted here.
    /// </param>
    /// <response code="200">
    /// Example: <c>{ "range": "24h", "fromUtc": "...", "toUtc": "...",
    /// "totals": { "totalEvents": 120, "allowedEvents": 90, "blockedEvents": 20, "auditOnlyEvents": 8, "errorEvents": 2 },
    /// "previousTotals": { ... same shape ... },
    /// "series": [ { "bucketStartUtc": "...", "total": 5, "blocked": 1, "allowed": 4, "auditOnly": 0, "errors": 0 }, ... ] }</c>
    /// </response>
    /// <response code="400">Invalid or unsupported range value.</response>
    [HttpGet("kpis")]
    public async Task<ActionResult<DlpKpisDto>> GetKpis([FromQuery] string? range, CancellationToken cancellationToken)
    {
        if (!DashboardRange.TryParse(range, DateTimeOffset.UtcNow, out var parsedRange, out var error))
        {
            return BadRequest(new { message = error });
        }

        var organizationId = User.GetOrganizationId();
        var cacheKey = DashboardCacheKeys.For(organizationId, "kpis", range: parsedRange.Key);

        var stopwatch = Stopwatch.StartNew();
        var result = await _cache.GetOrCreateWithLockAsync(
            cacheKey,
            KpisCacheDuration,
            () => _dashboardQueryService.GetKpisAsync(organizationId, parsedRange, cancellationToken));
        stopwatch.Stop();

        _logger.LogInformation(
            "dashboard.kpis org={OrganizationId} range={Range} elapsedMs={ElapsedMs}",
            organizationId, parsedRange.Key, stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// Point-in-time snapshot: pending/approved/rejected permission request counts, and the
    /// % of enrolled devices with a recent agent heartbeat. No range - this is "right now", not a
    /// historical window.
    /// </summary>
    /// <response code="200">
    /// Example: <c>{ "permissionRequestCounts": { "pending": 3, "approved": 40, "rejected": 5 },
    /// "endpointAgents": { "enrolledDeviceCount": 50, "heartbeatFreshDeviceCount": 47, "agentHeartbeatPct": 94.0 } }</c>
    /// </response>
    [HttpGet("status")]
    public async Task<ActionResult<DlpDashboardStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var organizationId = User.GetOrganizationId();
        var cacheKey = DashboardCacheKeys.For(organizationId, "status");

        var stopwatch = Stopwatch.StartNew();
        var result = await _cache.GetOrCreateWithLockAsync(
            cacheKey,
            StatusCacheDuration,
            () => _dashboardQueryService.GetStatusAsync(organizationId, cancellationToken));
        stopwatch.Stop();

        _logger.LogInformation(
            "dashboard.status org={OrganizationId} elapsedMs={ElapsedMs}",
            organizationId, stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// The most recent audit events for the window, newest first.
    /// </summary>
    /// <param name="range">One of <c>6h</c>, <c>24h</c>, <c>7d</c>. Defaults to <c>24h</c>.</param>
    /// <param name="limit">Defaults to 10; clamped to the 1-20 range server-side.</param>
    /// <response code="200">
    /// A JSON array, each item shaped like:
    /// <c>{ "actionKey": "usb.storage", "eventType": "Permission Evaluated", "username": "jdoe",
    /// "userSid": "S-1-5-...", "deviceId": "...", "deviceName": "LAPTOP-123", "details": "...",
    /// "decision": "Block", "reasonCode": "PolicyBlocked", "policyVersion": 7,
    /// "occurredAtUtc": "...", "receivedAtUtc": "..." }</c>. <c>deviceName</c> is null if the
    /// device no longer exists.
    /// </response>
    /// <response code="400">Invalid or unsupported range value.</response>
    [HttpGet("recent-events")]
    public async Task<ActionResult<IReadOnlyList<DlpRecentAuditEventWithDeviceDto>>> GetRecentEvents(
        [FromQuery] string? range,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (!DashboardRange.TryParse(range, DateTimeOffset.UtcNow, out var parsedRange, out var error))
        {
            return BadRequest(new { message = error });
        }

        var organizationId = User.GetOrganizationId();
        var clampedLimit = Math.Clamp(limit is null or <= 0 ? 10 : limit.Value, 1, 20);
        var cacheKey = DashboardCacheKeys.For(organizationId, "recent-events", range: parsedRange.Key, limit: clampedLimit);

        var stopwatch = Stopwatch.StartNew();
        var result = await _cache.GetOrCreateWithLockAsync(
            cacheKey,
            RecentEventsCacheDuration,
            () => _dashboardQueryService.GetRecentEventsAsync(organizationId, parsedRange, clampedLimit, cancellationToken));
        stopwatch.Stop();

        _logger.LogInformation(
            "dashboard.recent-events org={OrganizationId} range={Range} limit={Limit} elapsedMs={ElapsedMs}",
            organizationId, parsedRange.Key, clampedLimit, stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// Action/reason/channel breakdowns for the window - action and reason capped to the top 8.
    /// </summary>
    /// <param name="range">One of <c>6h</c>, <c>24h</c>, <c>7d</c>. Defaults to <c>24h</c>.</param>
    /// <response code="200">
    /// Example: <c>{ "actionBreakdown": [ { "actionKey": "usb.storage", "total": 40, "allowed": 30,
    /// "blocked": 8, "errors": 2, "lastReceivedAtUtc": "..." }, ... up to 8 ],
    /// "reasonBreakdown": [ { "reasonCode": "PolicyBlocked", "total": 8 }, ... up to 8 ],
    /// "channelBreakdown": [ { "category": "RemovableStorage", "blocked": 5 }, ... ] }</c>
    /// </response>
    /// <response code="400">Invalid or unsupported range value.</response>
    [HttpGet("breakdown")]
    public async Task<ActionResult<DlpBreakdownDto>> GetBreakdown([FromQuery] string? range, CancellationToken cancellationToken)
    {
        if (!DashboardRange.TryParse(range, DateTimeOffset.UtcNow, out var parsedRange, out var error))
        {
            return BadRequest(new { message = error });
        }

        var organizationId = User.GetOrganizationId();
        var cacheKey = DashboardCacheKeys.For(organizationId, "breakdown", range: parsedRange.Key);

        var stopwatch = Stopwatch.StartNew();
        var result = await _cache.GetOrCreateWithLockAsync(
            cacheKey,
            BreakdownCacheDuration,
            () => _dashboardQueryService.GetBreakdownAsync(organizationId, parsedRange, cancellationToken));
        stopwatch.Stop();

        _logger.LogInformation(
            "dashboard.breakdown org={OrganizationId} range={Range} elapsedMs={ElapsedMs}",
            organizationId, parsedRange.Key, stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// Top risky users and per-department risk, over a fixed rolling 30-day window. No range.
    /// </summary>
    /// <response code="200">
    /// Example: <c>{ "topRiskyUsers": [ { "employeeId": "...", "displayName": "Jane Doe",
    /// "departmentName": "Finance", "riskScore": 82, "riskLevel": "High",
    /// "topFactors": [ { "category": "RemovableStorage", "count": 6 } ] }, ... up to 10 ],
    /// "riskByDepartment": [ { "departmentName": "Finance", "riskScore": 55 }, ... ],
    /// "highRiskUserCount": 3 }</c>
    /// </response>
    [HttpGet("risk")]
    public async Task<ActionResult<DlpRiskDto>> GetRisk(CancellationToken cancellationToken)
    {
        var organizationId = User.GetOrganizationId();
        var cacheKey = DashboardCacheKeys.For(organizationId, "risk");

        var stopwatch = Stopwatch.StartNew();
        var result = await _cache.GetOrCreateWithLockAsync(
            cacheKey,
            RiskCacheDuration,
            () => _dashboardQueryService.GetRiskAsync(organizationId, cancellationToken));
        stopwatch.Stop();

        _logger.LogInformation(
            "dashboard.risk org={OrganizationId} elapsedMs={ElapsedMs}",
            organizationId, stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// Block/violation counts per 7-day bucket over a fixed rolling 28-day window (oldest bucket
    /// first). No range.
    /// </summary>
    /// <response code="200">
    /// Example: <c>{ "weeklyTrend": [ { "weekStartUtc": "...", "blockedCount": 4, "violationCount": 9 },
    /// ... 4 points, oldest first ] }</c>
    /// </response>
    [HttpGet("weekly-trend")]
    public async Task<ActionResult<DlpWeeklyTrendResponseDto>> GetWeeklyTrend(CancellationToken cancellationToken)
    {
        var organizationId = User.GetOrganizationId();
        var cacheKey = DashboardCacheKeys.For(organizationId, "weekly-trend");

        var stopwatch = Stopwatch.StartNew();
        var weeklyTrend = await _cache.GetOrCreateWithLockAsync(
            cacheKey,
            WeeklyTrendCacheDuration,
            () => _dashboardQueryService.GetWeeklyTrendAsync(organizationId, cancellationToken));
        stopwatch.Stop();

        _logger.LogInformation(
            "dashboard.weekly-trend org={OrganizationId} elapsedMs={ElapsedMs}",
            organizationId, stopwatch.ElapsedMilliseconds);

        return Ok(new DlpWeeklyTrendResponseDto(weeklyTrend));
    }
}
