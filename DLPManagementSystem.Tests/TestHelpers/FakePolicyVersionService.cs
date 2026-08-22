using DLPManagementSystem.Service.Interface;

namespace DLPManagementSystem.Tests.TestHelpers
{
    // No-op stand-in for the policy-version-bump side effect - DeviceService/PermissionGrantService
    // call this after every mutation, but none of these tests assert on policy version numbers.
    public sealed class FakePolicyVersionService : IPolicyVersionService
    {
        public Task BumpAsync(
            Guid organizationId,
            Guid? changedByUserId,
            string changeType,
            string entityType,
            Guid? entityId,
            string description,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
