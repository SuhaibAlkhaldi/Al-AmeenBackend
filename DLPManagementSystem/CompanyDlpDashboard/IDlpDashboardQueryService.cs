namespace DLPManagementSystem.CompanyDlpDashboard;

public interface IDlpDashboardQueryService
{
    Task<DlpDashboardSummaryDto> GetSummaryAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken);
}
