using DLPManagementSystem.DTO.AgentPolicy;
using DLPManagementSystem.Service.Service;

namespace DLPManagementSystem.Tests.TestHelpers
{
    // No-op stand-in for real ECDSA policy signing - these tests assert on which grants/data make it
    // into the snapshot, never on the signature itself.
    public sealed class FakePolicySigningService : IPolicySigningService
    {
        public bool HasRealKey => false;

        public (string Algorithm, string SignatureBase64) Sign(AgentPolicySnapshotDto snapshot) =>
            ("NONE", "DEVELOPMENT-UNSIGNED");
    }
}
