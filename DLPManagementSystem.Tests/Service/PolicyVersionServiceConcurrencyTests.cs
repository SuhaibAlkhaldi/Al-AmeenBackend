using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the fix for the PolicyVersionService.BumpAsync race condition: two concurrent callers used
    // to both read the same MAX(VersionNumber) and then both try to insert that value + 1, tripping
    // UQ_PolicyVersions_Organization_Version and surfacing to the caller as an unhandled 500 for an
    // otherwise entirely valid operation (device assignment, permission grant/revoke, ...). BumpAsync now
    // draws its number from a real SQL Server SEQUENCE via NEXT VALUE FOR.
    //
    // This deliberately does NOT use TestDbContextFactory's InMemory provider like the rest of this test
    // project - the InMemory provider has no concept of a SQL Server SEQUENCE and doesn't support raw SQL
    // queries, so it's structurally incapable of exercising the code path under test. Instead this connects
    // to the real local dev SQL Server database (same one the rest of this session's manual testing used) -
    // the login this project ships with (ameen_dev_login) only has rights on that database, not
    // CREATE DATABASE on the server, so a disposable throwaway database per test run isn't available here.
    // Each test creates its own throwaway Organization row (and cleans up everything it wrote), so it's
    // safe to run against the same database other things (including the live dev backend) may be using
    // concurrently - it never touches any pre-existing row.
    //
    // Because the sequence is genuinely shared with whatever else is live on this database, this does NOT
    // assert the claimed numbers are gap-free/contiguous (unrelated concurrent activity elsewhere could
    // legitimately consume values in between) - only that they are unique, which is the actual correctness
    // property the fix guarantees.
    public sealed class PolicyVersionServiceConcurrencyTests : IAsyncLifetime
    {
        private const string ConnectionString =
            "Server=localhost,1433;Database=DLPSystem;User Id=ameen_dev_login;Password=AmeenDev_2026!;TrustServerCertificate=True;Connect Timeout=60;";

        private Guid _organizationId;

        private static DLPSystemContext CreateContext() =>
            new(new DbContextOptionsBuilder<DLPSystemContext>().UseSqlServer(ConnectionString).Options);

        public async Task InitializeAsync()
        {
            await using var db = CreateContext();
            _organizationId = Guid.NewGuid();
            db.Organizations.Add(new Organization
            {
                Id = _organizationId,
                Name = "Concurrency Test Org",
                Code = $"CONC-{_organizationId:N}".Substring(0, 20),
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await using var db = CreateContext();
            // FK order: PolicyChangeLogs -> PolicyVersions -> Organizations.
            await db.PolicyChangeLogs.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.PolicyVersions.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Organizations.Where(x => x.Id == _organizationId).ExecuteDeleteAsync();
        }

        [Fact]
        public async Task BumpAsync_TwentyConcurrentCallsSameOrganization_ProducesTwentyUniqueNumbers()
        {
            const int concurrentCalls = 20;

            var tasks = Enumerable.Range(0, concurrentCalls).Select(async i =>
            {
                await using var db = CreateContext();
                var service = new PolicyVersionService(db);
                await service.BumpAsync(
                    _organizationId, null, "ConcurrencyTest", "Test", null,
                    $"concurrency-test-bump-{i}", CancellationToken.None);
                await db.SaveChangesAsync();
            });

            await Task.WhenAll(tasks);

            await using var verifyDb = CreateContext();
            var versionNumbers = await verifyDb.PolicyVersions
                .Where(x => x.OrganizationId == _organizationId)
                .Select(x => x.VersionNumber)
                .ToListAsync();

            Assert.Equal(concurrentCalls, versionNumbers.Count);
            Assert.Equal(concurrentCalls, versionNumbers.Distinct().Count());
        }

        [Fact]
        public async Task BumpAsync_ConcurrentCallsForDifferentOperations_BothSucceedWithDistinctNumbers()
        {
            // Mirrors the two real call sites that originally exposed this race: DeviceService assigning
            // a device and PermissionGrantService revoking a grant, landing at the same instant for the
            // same organization. Neither operation has anything to do with the other beyond both bumping
            // the same organization's policy version - which is exactly what used to collide.
            await using var deviceAssignmentDb = CreateContext();
            var deviceAssignmentTask = Task.Run(async () =>
            {
                var service = new PolicyVersionService(deviceAssignmentDb);
                await service.BumpAsync(
                    _organizationId, null, "DeviceAssigned", "Device", Guid.NewGuid(),
                    "Device assigned to employee.", CancellationToken.None);
                await deviceAssignmentDb.SaveChangesAsync();
            });

            await using var grantRevokedDb = CreateContext();
            var grantRevokedTask = Task.Run(async () =>
            {
                var service = new PolicyVersionService(grantRevokedDb);
                await service.BumpAsync(
                    _organizationId, null, "GrantRevoked", "PermissionGrant", Guid.NewGuid(),
                    "Permission grant revoked.", CancellationToken.None);
                await grantRevokedDb.SaveChangesAsync();
            });

            var exception = await Record.ExceptionAsync(() => Task.WhenAll(deviceAssignmentTask, grantRevokedTask));
            Assert.Null(exception);

            await using var verifyDb = CreateContext();
            var versions = await verifyDb.PolicyVersions
                .Where(x => x.OrganizationId == _organizationId)
                .ToListAsync();

            Assert.Equal(2, versions.Count);
            Assert.Equal(2, versions.Select(x => x.VersionNumber).Distinct().Count());
        }
    }
}
