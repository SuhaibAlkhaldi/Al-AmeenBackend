namespace DLPManagementSystem.CompanyDlpDashboard;

public interface IDlpDashboardQueryService
{
    Task<DlpDashboardSummaryDto> GetSummaryAsync(
        Guid organizationId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken);

    // --- dashboard-split-endpoints: one independent query per split endpoint, each opening its own
    // SqlConnection (see SqlDlpDashboardQueryService) so a slow one never blocks the others. ---

    Task<DlpKpisDto> GetKpisAsync(
        Guid organizationId,
        DashboardRange range,
        CancellationToken cancellationToken);

    Task<DlpDashboardStatusDto> GetStatusAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DlpRecentAuditEventWithDeviceDto>> GetRecentEventsAsync(
        Guid organizationId,
        DashboardRange range,
        int limit,
        CancellationToken cancellationToken);

    Task<DlpBreakdownDto> GetBreakdownAsync(
        Guid organizationId,
        DashboardRange range,
        CancellationToken cancellationToken);

    Task<DlpRiskDto> GetRiskAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DlpWeeklyTrendPointDto>> GetWeeklyTrendAsync(
        Guid organizationId,
        CancellationToken cancellationToken);
}
