using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DLPManagementSystem.Service.Service
{
    public sealed class PermissionLookupService : IPermissionLookupService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly DLPSystemContext _db;
        private readonly IMemoryCache _cache;

        public PermissionLookupService(DLPSystemContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public Task<int> GetPermissionDecisionId(string name, CancellationToken cancellationToken)
        {
            return GetLookupIdAsync(
                cacheKey: $"PermissionDecision:{name}",
                lookupName: "PermissionDecisions",
                valueName: name,
                query: () => _db.PermissionDecisions
                    .AsNoTracking()
                    .Where(x => x.Name == name)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken));
        }

        public Task<int> GetPermissionGrantTypeId(string name, CancellationToken cancellationToken)
        {
            return GetLookupIdAsync(
                cacheKey: $"PermissionGrantType:{name}",
                lookupName: "PermissionGrantTypes",
                valueName: name,
                query: () => _db.PermissionGrantTypes
                    .AsNoTracking()
                    .Where(x => x.Name == name)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken));
        }

        public Task<int> GetPermissionSubjectTypeId(string name, CancellationToken cancellationToken)
        {
            return GetLookupIdAsync(
                cacheKey: $"PermissionSubjectType:{name}",
                lookupName: "PermissionSubjectTypes",
                valueName: name,
                query: () => _db.PermissionSubjectTypes
                    .AsNoTracking()
                    .Where(x => x.Name == name)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken));
        }

        public Task<int> GetPermissionRequestStatusId(string name, CancellationToken cancellationToken)
        {
            return GetLookupIdAsync(
                cacheKey: $"PermissionRequestStatus:{name}",
                lookupName: "PermissionRequestStatuses",
                valueName: name,
                query: () => _db.PermissionRequestStatuses
                    .AsNoTracking()
                    .Where(x => x.Name == name)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken));
        }

        public Task<int> GetPermissionRequestReviewDecisionId(string name, CancellationToken cancellationToken)
        {
            return GetLookupIdAsync(
                cacheKey: $"PermissionRequestReviewDecision:{name}",
                lookupName: "PermissionRequestReviewDecisions",
                valueName: name,
                query: () => _db.PermissionRequestReviewDecisions
                    .AsNoTracking()
                    .Where(x => x.Name == name)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken));
        }

        private async Task<int> GetLookupIdAsync(
            string cacheKey,
            string lookupName,
            string valueName,
            Func<Task<int?>> query)
        {
            var cachedValue = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                var id = await query();

                if (id is null)
                {
                    throw new InvalidOperationException(
                        $"Required lookup value '{valueName}' was not found in '{lookupName}'.");
                }

                return id.Value;
            });

            return cachedValue;
        }
    }
}
