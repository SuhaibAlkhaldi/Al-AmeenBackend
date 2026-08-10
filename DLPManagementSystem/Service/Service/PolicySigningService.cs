using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using DLPManagementSystem.DTO.AgentPolicy;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Service.Service
{
    public sealed class PolicySigningOptions
    {
        public string PrivateKeyPem { get; set; } = "";
    }

    // Byte-for-byte identical to CompanyDlp.Contracts.JsonDefaults.Options on the Windows agent -
    // PolicySnapshotValidator re-serializes its deserialized snapshot with those exact options to
    // verify the signature, so signing here with anything else guarantees verification failure
    // regardless of whether the DTO shapes themselves are correct.
    public static class PolicySigningJson
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public interface IPolicySigningService
    {
        // Must be checked by callers BEFORE constructing the snapshot to sign: Sign()'s outcome
        // (whether it produces a real ECDSA-SHA256 signature or falls back to the DEVELOPMENT-UNSIGNED
        // sentinel) and the snapshot's own Policy.Runtime.Mode must never be set independently of each
        // other again - that divergence (Runtime.Mode hardcoded to "Production" regardless of whether
        // signing actually happened) is exactly what made every Development-mode agent's policy
        // snapshot unverifiable. See PolicySnapshotValidator.TryValidate on the agent side, whose
        // unsigned-dev bypass requires both facts to agree.
        bool HasRealKey { get; }

        (string Algorithm, string SignatureBase64) Sign(AgentPolicySnapshotDto snapshot);
    }

    // Signs exactly the same 6 fields, in the same order, that CompanyDlp.Service.PolicySnapshotValidator
    // (agent side, not modified by this change) reconstructs to verify: PolicyId, Version, TenantId,
    // DeviceId, IssuedAtUtc, ExpiresAtUtc, Policy - see that class for the ground-truth verification code
    // this must match.
    public sealed class EcdsaPolicySigningService : IPolicySigningService
    {
        private readonly ECDsa? _ecdsa;

        public bool HasRealKey => _ecdsa is not null;

        public EcdsaPolicySigningService(
            IOptions<PolicySigningOptions> options,
            Microsoft.Extensions.Logging.ILogger<EcdsaPolicySigningService> logger)
        {
            var pem = options.Value.PrivateKeyPem;
            if (string.IsNullOrWhiteSpace(pem))
            {
                _ecdsa = null;
                return;
            }

            try
            {
                var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(pem);
                _ecdsa = ecdsa;
            }
            catch (Exception exception)
            {
                // Never crash the app over a bad dev-time key - Program.cs's own startup check is what
                // enforces this must be valid in Production. Here, just fail closed to "unsigned" so a
                // misconfigured Development environment behaves exactly as it always has.
                logger.LogError(exception, "PolicySigning:PrivateKeyPem is configured but is not a valid ECDSA private key. Policies will be sent unsigned.");
                _ecdsa = null;
            }
        }

        public (string Algorithm, string SignatureBase64) Sign(AgentPolicySnapshotDto snapshot)
        {
            if (_ecdsa is null)
            {
                // No real key configured (typical local Development) - fall back to the same sentinel
                // PolicySnapshotValidator has always recognized as its "unsigned Development" fast path.
                return ("DEVELOPMENT", "DEVELOPMENT-UNSIGNED");
            }

            var payload = JsonSerializer.SerializeToUtf8Bytes(new
            {
                snapshot.PolicyId,
                snapshot.Version,
                snapshot.TenantId,
                snapshot.DeviceId,
                snapshot.IssuedAtUtc,
                snapshot.ExpiresAtUtc,
                snapshot.Policy
            }, PolicySigningJson.Options);

            var signature = _ecdsa.SignData(payload, HashAlgorithmName.SHA256);
            return ("ECDSA-SHA256", Convert.ToBase64String(signature));
        }
    }
}
