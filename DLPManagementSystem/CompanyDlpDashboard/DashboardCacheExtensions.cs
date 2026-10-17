using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace DLPManagementSystem.CompanyDlpDashboard;

// Microsoft.Extensions.Caching.Memory's own IMemoryCache.GetOrCreateAsync has no single-flight
// protection: N concurrent misses on the same key all run the factory and all race to Set() the
// result - wasted duplicate DB work, exactly what per-endpoint caching is meant to avoid under a
// dashboard page load that fires several of these at once. This adds one SemaphoreSlim per cache
// key so only the first caller actually queries; the rest wait for it and then read its result.
//
// The semaphores themselves (one per distinct key, never per request) live for the process
// lifetime - unbounded only in the sense that the key space is unbounded (OrganizationId x
// endpoint x range/limit), which in practice is small and stable per tenant, not per request.
public static class DashboardCacheExtensions
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

    public static async Task<T> GetOrCreateWithLockAsync<T>(
        this IMemoryCache cache,
        string key,
        TimeSpan duration,
        Func<Task<T>> factory)
    {
        if (cache.TryGetValue(key, out T? cached))
        {
            return cached!;
        }

        var gate = Gates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (cache.TryGetValue(key, out cached))
            {
                return cached!;
            }

            // Not cached on failure: an exception here propagates straight to the caller without
            // ever reaching cache.Set, so a transient DB error is never "sticky" for the TTL.
            var value = await factory();
            cache.Set(key, value, duration);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }
}

// Cache key builder - OrganizationId first (so one org's keys never collide with another's,
// regardless of endpoint/range naming) then the endpoint name, then any sub-parameters (range,
// limit) that vary the response shape/content for an otherwise-identical endpoint+org pair.
public static class DashboardCacheKeys
{
    public static string For(Guid organizationId, string endpoint, string? range = null, int? limit = null)
    {
        var key = $"dashboard:{organizationId:N}:{endpoint}";
        if (range is not null)
        {
            key += $":range={range}";
        }

        if (limit is not null)
        {
            key += $":limit={limit}";
        }

        return key;
    }
}
