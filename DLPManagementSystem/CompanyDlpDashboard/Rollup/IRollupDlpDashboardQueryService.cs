namespace DLPManagementSystem.CompanyDlpDashboard.Rollup;

// dashboard-rollup: the four endpoints that CAN be served from the rollup tables - kpis,
// breakdown, weekly-trend, risk. status/recent-events/summary never read from rollup (see
// RollupAwareDlpDashboardQueryService, the only class that talks to this interface) so they are
// deliberately not part of this contract at all.
public interface IRollupDlpDashboardQueryService
{
    Task<DlpKpisDto> GetKpisAsync(Guid organizationId, DashboardRange range, CancellationToken cancellationToken);

    Task<DlpBreakdownDto> GetBreakdownAsync(Guid organizationId, DashboardRange range, CancellationToken cancellationToken);

    Task<IReadOnlyList<DlpWeeklyTrendPointDto>> GetWeeklyTrendAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<DlpRiskDto> GetRiskAsync(Guid organizationId, CancellationToken cancellationToken);
}
