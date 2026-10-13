using DLPManagementSystem.CompanyDlpDashboard.Rollup;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Service.BackgroundServices
{
    // dashboard-rollup: periodically re-aggregates the trailing window of raw AuditEvents into
    // DashboardHourlyRollup/DashboardEmployeeDailyRollup via IDashboardRollupRefreshService - see
    // that service's own comments for the locking/visibility design. Off by default
    // (DashboardRollup:RefreshEnabled) - same "do nothing unless explicitly configured" convention
    // as AuditRetentionWorker. Deliberately independent of DashboardRollup:UseRollup (the read-path
    // flag): the rollout plan in DASHBOARD_ENDPOINTS.md turns this worker on well before flipping
    // UseRollup, so the tables are already warm and verified against the raw path first.
    public sealed class DashboardRollupRefreshWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<DashboardRollupOptions> _options;
        private readonly ILogger<DashboardRollupRefreshWorker> _logger;

        public DashboardRollupRefreshWorker(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<DashboardRollupOptions> options,
            ILogger<DashboardRollupRefreshWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var options = _options.CurrentValue;

                if (!options.RefreshEnabled)
                {
                    _logger.LogDebug("Dashboard rollup refresh is disabled (DashboardRollup:RefreshEnabled=false); skipping.");
                }
                else
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var refreshService = scope.ServiceProvider.GetRequiredService<IDashboardRollupRefreshService>();
                        await refreshService.RefreshRecentWindowAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // A single bad tick (a transient SQL error, a timeout) must never bring the
                        // whole application down - logged and retried next interval, same tolerance
                        // AuditRetentionWorker's own loop already has.
                        _logger.LogError(ex, "Unexpected error during a dashboard rollup refresh cycle.");
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.RefreshIntervalSeconds)), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown.
                }
            }
        }
    }
}
