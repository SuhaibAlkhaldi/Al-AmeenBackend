namespace DLPManagementSystem.CompanyDlpDashboard;

// Response DTOs for the six split dashboard endpoints (dashboard-split-endpoints) - kept in their
// own file, separate from DlpDashboardDtos.cs, so the existing GET /api/v1/dashboard/summary
// contract (DlpDashboardSummaryDto and friends) is never touched. Where a shape matches an
// existing summary sub-DTO exactly (DlpRiskUserDto, DlpDashboardSeriesPointDto,
// DlpPermissionRequestCountsDto, DlpEndpointAgentStatusDto, DlpActionBreakdownItemDto,
// DlpReasonBreakdownItemDto, DlpChannelBreakdownItemDto, DlpDepartmentRiskDto,
// DlpWeeklyTrendPointDto), it's reused as-is instead of duplicated here.

// GET /api/v1/dashboard/kpis?range=6h|24h|7d
// Totals here deliberately drop UniqueActions/LastReceivedAtUtc/PolicyVersion from
// DlpDashboardTotalsDto - unused by the frontend, and cheaper to not compute per ui-data-batch3's
// GetTotalsAsync.
public sealed record DlpKpisDto(
    string Range,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    DlpKpiTotalsDto Totals,
    DlpKpiTotalsDto PreviousTotals,
    IReadOnlyList<DlpDashboardSeriesPointDto> Series);

public sealed record DlpKpiTotalsDto(
    long TotalEvents,
    long AllowedEvents,
    long BlockedEvents,
    long AuditOnlyEvents,
    long ErrorEvents);

// GET /api/v1/dashboard/status - a point-in-time snapshot, no range.
public sealed record DlpDashboardStatusDto(
    DlpPermissionRequestCountsDto PermissionRequestCounts,
    DlpEndpointAgentStatusDto EndpointAgents);

// GET /api/v1/dashboard/recent-events?range=...&limit=10 - same fields as the summary's
// DlpRecentAuditEventDto, plus DeviceName (resolved from dbo.Devices.MachineName via DeviceId;
// null if the device no longer exists).
public sealed record DlpRecentAuditEventWithDeviceDto(
    string ActionKey,
    string? EventType,
    string? Username,
    string? UserSid,
    string? DeviceId,
    string? DeviceName,
    string? Details,
    string? Decision,
    string? ReasonCode,
    long? PolicyVersion,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ReceivedAtUtc);

// GET /api/v1/dashboard/breakdown?range=... - action/reason breakdowns capped to the top 8 (vs the
// summary's top 30/15) since this is a focused, standalone view rather than a feed into a bigger
// payload.
public sealed record DlpBreakdownDto(
    IReadOnlyList<DlpActionBreakdownItemDto> ActionBreakdown,
    IReadOnlyList<DlpReasonBreakdownItemDto> ReasonBreakdown,
    IReadOnlyList<DlpChannelBreakdownItemDto> ChannelBreakdown);

// GET /api/v1/dashboard/risk - fixed rolling 30-day window, no range.
public sealed record DlpRiskDto(
    IReadOnlyList<DlpRiskUserDto> TopRiskyUsers,
    IReadOnlyList<DlpDepartmentRiskDto> RiskByDepartment,
    int HighRiskUserCount);

// GET /api/v1/dashboard/weekly-trend - fixed rolling 28-day window, no range.
public sealed record DlpWeeklyTrendResponseDto(
    IReadOnlyList<DlpWeeklyTrendPointDto> WeeklyTrend);
