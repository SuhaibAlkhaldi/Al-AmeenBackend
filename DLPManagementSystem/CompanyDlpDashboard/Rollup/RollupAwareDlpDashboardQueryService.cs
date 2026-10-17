using Microsoft.Extensions.Options;

namespace DLPManagementSystem.CompanyDlpDashboard.Rollup;

// dashboard-rollup: the ONLY class registered as IDlpDashboardQueryService (see Program.cs) -
// every controller action keeps calling the same interface it always has, unaware this decorator
// exists. Routes exactly the four endpoints the rollup can serve (kpis/breakdown/weekly-trend/
// risk) to either the raw SqlDlpDashboardQueryService or the new RollupDlpDashboardQueryService
// based on DashboardRollup:UseRollup - summary/status/recent-events always go straight to the raw
// implementation, flag or no flag, because they either must never change (summary) or were never
// part of the rollup's scope (status/recent-events - see DASHBOARD_ENDPOINTS.md's "Rollup"
// section for why). Flipping UseRollup back to false at any time fully reverts to the pre-rollup
// behavior - this class holds no state of its own.
public sealed class RollupAwareDlpDashboardQueryService : IDlpDashboardQueryService
{
    private readonly SqlDlpDashboardQueryService _raw;
    private readonly IRollupDlpDashboardQueryService _rollup;
    private readonly IOptionsMonitor<DashboardRollupOptions> _options;

    public RollupAwareDlpDashboardQueryService(
        SqlDlpDashboardQueryService raw,
        IRollupDlpDashboardQueryService rollup,
        IOptionsMonitor<DashboardRollupOptions> options)
    {
        _raw = raw;
        _rollup = rollup;
        _options = options;
    }

    private bool UseRollup => _options.CurrentValue.UseRollup;

    public Task<DlpDashboardSummaryDto> GetSummaryAsync(
        Guid organizationId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken cancellationToken) =>
        _raw.GetSummaryAsync(organizationId, fromUtc, toUtc, cancellationToken);

    public Task<DlpDashboardStatusDto> GetStatusAsync(Guid organizationId, CancellationToken cancellationToken) =>
        _raw.GetStatusAsync(organizationId, cancellationToken);

    public Task<IReadOnlyList<DlpRecentAuditEventWithDeviceDto>> GetRecentEventsAsync(
        Guid organizationId, DashboardRange range, int limit, CancellationToken cancellationToken) =>
        _raw.GetRecentEventsAsync(organizationId, range, limit, cancellationToken);

    public Task<DlpKpisDto> GetKpisAsync(Guid organizationId, DashboardRange range, CancellationToken cancellationToken) =>
        UseRollup
            ? _rollup.GetKpisAsync(organizationId, range, cancellationToken)
            : _raw.GetKpisAsync(organizationId, range, cancellationToken);

    public Task<DlpBreakdownDto> GetBreakdownAsync(Guid organizationId, DashboardRange range, CancellationToken cancellationToken) =>
        UseRollup
            ? _rollup.GetBreakdownAsync(organizationId, range, cancellationToken)
            : _raw.GetBreakdownAsync(organizationId, range, cancellationToken);

    public Task<IReadOnlyList<DlpWeeklyTrendPointDto>> GetWeeklyTrendAsync(Guid organizationId, CancellationToken cancellationToken) =>
        UseRollup
            ? _rollup.GetWeeklyTrendAsync(organizationId, cancellationToken)
            : _raw.GetWeeklyTrendAsync(organizationId, cancellationToken);

    public Task<DlpRiskDto> GetRiskAsync(Guid organizationId, CancellationToken cancellationToken) =>
        UseRollup
            ? _rollup.GetRiskAsync(organizationId, cancellationToken)
            : _raw.GetRiskAsync(organizationId, cancellationToken);
}
