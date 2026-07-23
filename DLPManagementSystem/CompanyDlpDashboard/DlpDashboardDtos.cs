namespace DLPManagementSystem.CompanyDlpDashboard;

public sealed record DlpDashboardSummaryDto(
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    long? PolicyVersion,
    DlpDashboardTotalsDto Totals,
    IReadOnlyList<DlpActionBreakdownItemDto> ActionBreakdown,
    IReadOnlyList<DlpReasonBreakdownItemDto> ReasonBreakdown,
    IReadOnlyList<DlpRecentAuditEventDto> RecentEvents,
    DlpPermissionRequestCountsDto PermissionRequestCounts,
    IReadOnlyList<DlpWeeklyTrendPointDto> WeeklyTrend,
    IReadOnlyList<DlpChannelBreakdownItemDto> ChannelBreakdown,
    IReadOnlyList<DlpRiskUserDto> TopRiskyUsers,
    IReadOnlyList<DlpDepartmentRiskDto> RiskByDepartment,
    int HighRiskUserCount,
    DlpEndpointAgentStatusDto EndpointAgents);

public sealed record DlpPermissionRequestCountsDto(
    long Pending,
    long Approved,
    long Rejected);

// One point per rolling 7-day bucket over the last 4 weeks (independent of the summary's from/to
// range toggle — a fixed, longer-horizon trend view). BlockedCount = Block decisions only.
// ViolationCount = Block + AuditOnly combined, since audit-only events are still policy-flagged,
// just not prevented.
public sealed record DlpWeeklyTrendPointDto(
    DateTimeOffset WeekStartUtc,
    long BlockedCount,
    long ViolationCount);

// Blocked-event count per real tracked channel category (PermissionActionCategories), for the
// summary's from/to window. Deliberately excludes the "System" category (policy.apply/agent.session
// housekeeping events) since those aren't user-facing violations — see PROMPT_DASHBOARD_REDESIGN.
public sealed record DlpChannelBreakdownItemDto(
    string Category,
    long Blocked);

public sealed record DlpRiskFactorDto(
    string Category,
    long Count);

// v1 risk score definition (see SqlDlpDashboardQueryService.ComputeRiskScores for the full comment):
// a normalized 0-100 score based on an employee's Block-decision event count over a fixed 30-day
// window, relative to the highest count in the org. Not an established/audited metric — a documented
// starting point, adjustable later.
public sealed record DlpRiskUserDto(
    Guid EmployeeId,
    string DisplayName,
    string? DepartmentName,
    int RiskScore,
    string RiskLevel,
    IReadOnlyList<DlpRiskFactorDto> TopFactors);

public sealed record DlpDepartmentRiskDto(
    string DepartmentName,
    int RiskScore);

// Backs the "Endpoint Agents" hero card — % of enrolled devices with a heartbeat within
// HeartbeatFreshMinutes. The only real decision-relevant signal that used to live in the removed
// System Status panel; Database/Backend "online" checks were dropped as near-zero decision value
// (trivially true if this endpoint responded at all) — see PROMPT_DASHBOARD_ENDPOINT_AGENTS_CARD.
public sealed record DlpEndpointAgentStatusDto(
    int EnrolledDeviceCount,
    int HeartbeatFreshDeviceCount,
    double AgentHeartbeatPct);

public sealed record DlpDashboardTotalsDto(
    long TotalEvents,
    long AllowedEvents,
    long BlockedEvents,
    long AuditOnlyEvents,
    long ErrorEvents,
    int UniqueActions,
    DateTimeOffset? LastReceivedAtUtc);

public sealed record DlpActionBreakdownItemDto(
    string ActionKey,
    long Total,
    long Allowed,
    long Blocked,
    long Errors,
    DateTimeOffset? LastReceivedAtUtc);

public sealed record DlpReasonBreakdownItemDto(
    string ReasonCode,
    long Total);

public sealed record DlpRecentAuditEventDto(
    string ActionKey,
    string? EventType,
    string? Username,
    string? UserSid,
    string? DeviceId,
    string? Details,
    string? Decision,
    string? ReasonCode,
    long? PolicyVersion,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ReceivedAtUtc);
