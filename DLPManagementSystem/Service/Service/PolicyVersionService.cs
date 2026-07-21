using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    // Stages a new PolicyVersion + PolicyChangeLog row so the next agent policy poll for this org
    // picks up a change (grant approved/revoked, device reassigned, etc.). Callers must still call
    // SaveChangesAsync themselves — this only adds to the tracked context, it doesn't persist alone,
    // so the version bump commits atomically together with whatever change triggered it.
    public sealed class PolicyVersionService : IPolicyVersionService
    {
        private readonly DLPSystemContext _db;

        public PolicyVersionService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task BumpAsync(
            Guid organizationId,
            Guid? changedByUserId,
            string changeType,
            string entityType,
            Guid? entityId,
            string description,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTimeOffset.UtcNow;

            var lastVersionNumber = await _db.PolicyVersions
                .Where(x => x.OrganizationId == organizationId)
                .OrderByDescending(x => x.VersionNumber)
                .Select(x => x.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var policyVersion = new PolicyVersion
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                VersionNumber = lastVersionNumber + 1,
                ChangedAtUtc = nowUtc,
                ChangedByUserId = changedByUserId,
                ChangeReason = description
            };

            _db.PolicyVersions.Add(policyVersion);

            _db.PolicyChangeLogs.Add(new PolicyChangeLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                PolicyVersionId = policyVersion.Id,
                ChangeType = changeType,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                ChangedByUserId = changedByUserId,
                ChangedAtUtc = nowUtc
            });
        }
    }
}
