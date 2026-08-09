using DLPManagementSystem.Service.Service;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DLPManagementSystem.Helper.Health
{
    public sealed class PolicySigningHealthCheck : IHealthCheck
    {
        private readonly IPolicySigningService _policySigningService;

        public PolicySigningHealthCheck(IPolicySigningService policySigningService)
        {
            _policySigningService = policySigningService;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Only ever reports HasRealKey (a bool) - never touches or logs the key material itself.
            var result = _policySigningService.HasRealKey
                ? HealthCheckResult.Healthy("Policy signing key is configured.")
                : HealthCheckResult.Unhealthy("Policy signing key is not configured or invalid.");

            return Task.FromResult(result);
        }
    }
}
