namespace DLPManagementSystem.CompanyDlpDashboard;

public interface IDlpDashboardQueryService
{
    Task<DlpDashboardSummaryDto> GetSummaryAsync(
        Guid organizationId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken);
}
