using DLPManagementSystem.Service.Service;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace DLPManagementSystem.Helper.Health
{
    public sealed class PolicySigningHealthCheck : IHealthCheck
    {
        private readonly IPolicySigningService _policySigningService;
        private readonly IHostEnvironment _environment;

        public PolicySigningHealthCheck(IPolicySigningService policySigningService, IHostEnvironment environment)
        {
            _policySigningService = policySigningService;
            _environment = environment;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // Only ever reports HasRealKey (a bool) - never touches or logs the key material itself.
            if (_policySigningService.HasRealKey)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Policy signing key is configured."));
            }

            // A missing/invalid key is only a real problem in Production - EcdsaPolicySigningService
            // falls back to signing policies as "DEVELOPMENT-UNSIGNED" (see Program.cs's comment right
            // above where this check is registered), and PolicySnapshotValidator on every agent only
            // ever accepts that sentinel when its own local AllowUnsignedDevelopmentPolicy is true and
            // the snapshot's Runtime.Mode is "Development" - exactly the local-dev setup this backend
            // is meant to support without a real keypair. Reporting Unhealthy here in Development blocks
            // /health/ready for a state the rest of the system already treats as safe and intentional.
            var result = _environment.IsProduction()
                ? HealthCheckResult.Unhealthy("Policy signing key is not configured or invalid.")
                : HealthCheckResult.Healthy("No policy signing key configured - falling back to unsigned Development policies.");

            return Task.FromResult(result);
        }
    }
}
