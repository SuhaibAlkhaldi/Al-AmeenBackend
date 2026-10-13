using DLPManagementSystem.CompanyDlpDashboard;
using Microsoft.Extensions.Caching.Memory;

namespace DLPManagementSystem.Tests.Service
{
    public class DashboardCacheExtensionsTests
    {
        [Fact]
        public async Task GetOrCreateWithLockAsync_CachesTheResultForTheGivenDuration()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var callCount = 0;

            async Task<int> Factory()
            {
                Interlocked.Increment(ref callCount);
                await Task.Yield();
                return 42;
            }

            var first = await cache.GetOrCreateWithLockAsync("key-a", TimeSpan.FromMinutes(1), Factory);
            var second = await cache.GetOrCreateWithLockAsync("key-a", TimeSpan.FromMinutes(1), Factory);

            Assert.Equal(42, first);
            Assert.Equal(42, second);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetOrCreateWithLockAsync_ConcurrentMissesOnTheSameKey_OnlyRunTheFactoryOnce()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var callCount = 0;
            var gate = new TaskCompletionSource();

            async Task<int> SlowFactory()
            {
                Interlocked.Increment(ref callCount);
                await gate.Task; // all concurrent callers block here until released together
                return 7;
            }

            var key = $"stampede-{Guid.NewGuid()}";
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => cache.GetOrCreateWithLockAsync(key, TimeSpan.FromMinutes(1), SlowFactory))
                .ToArray();

            // Give every task a chance to reach the semaphore before releasing the factory.
            await Task.Delay(50);
            gate.SetResult();

            var results = await Task.WhenAll(tasks);

            Assert.All(results, r => Assert.Equal(7, r));
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task GetOrCreateWithLockAsync_DifferentKeys_NeverShareACachedValue()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());

            var forOrgA = await cache.GetOrCreateWithLockAsync("org-a", TimeSpan.FromMinutes(1), () => Task.FromResult("A-data"));
            var forOrgB = await cache.GetOrCreateWithLockAsync("org-b", TimeSpan.FromMinutes(1), () => Task.FromResult("B-data"));

            Assert.Equal("A-data", forOrgA);
            Assert.Equal("B-data", forOrgB);
        }

        [Fact]
        public async Task GetOrCreateWithLockAsync_FailedFactory_IsNeverCached()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var key = $"failing-{Guid.NewGuid()}";

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                cache.GetOrCreateWithLockAsync<int>(key, TimeSpan.FromMinutes(1), () => throw new InvalidOperationException("boom")));

            // A second call after the failure must retry the factory, not replay a cached failure.
            var value = await cache.GetOrCreateWithLockAsync(key, TimeSpan.FromMinutes(1), () => Task.FromResult(99));

            Assert.Equal(99, value);
        }

        [Fact]
        public void For_IncludesOrganizationIdSoTwoOrganizationsNeverCollide()
        {
            var orgA = Guid.NewGuid();
            var orgB = Guid.NewGuid();

            var keyA = DashboardCacheKeys.For(orgA, "kpis", range: "24h");
            var keyB = DashboardCacheKeys.For(orgB, "kpis", range: "24h");

            Assert.NotEqual(keyA, keyB);
        }

        [Fact]
        public void For_IncludesRangeAndLimitSoDifferentParametersNeverCollide()
        {
            var org = Guid.NewGuid();

            var sixHour = DashboardCacheKeys.For(org, "recent-events", range: "6h", limit: 10);
            var sevenDay = DashboardCacheKeys.For(org, "recent-events", range: "7d", limit: 10);
            var limit20 = DashboardCacheKeys.For(org, "recent-events", range: "6h", limit: 20);

            Assert.NotEqual(sixHour, sevenDay);
            Assert.NotEqual(sixHour, limit20);
        }
    }
}
