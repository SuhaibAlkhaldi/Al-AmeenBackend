using DLPManagementSystem.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DLPManagementSystem.Helper.Health
{
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly DLPSystemContext _db;

        public DatabaseHealthCheck(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _db.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("SQL Server is reachable.")
                    : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy(
                    "SQL Server health check failed.",
                    exception);
            }
        }
    }
}
