using DLPManagementSystem.Common;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the ui-data-batch1 addition: EnrollmentTokenService.GetTokensAsync's new optional
    // `status` filter, and AgentEnrollmentTokenRuntimeStatus (the SQL-translatable expression behind
    // it) - mirrors PermissionGrantRuntimeStatusTests' shape. The Active/Exhausted/Expired/Revoked
    // classification itself must match EnrollmentTokensListComponent.status() on the frontend
    // exactly: Revoked beats Expired beats Exhausted beats Active.
    public class EnrollmentTokenServiceTests
    {
        private static User AddCreator(DLPSystemContext db)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                FullName = "Admin User",
                Email = $"{Guid.NewGuid():N}@example.local",
                PasswordHash = "irrelevant-for-this-test",
                RoleId = 1,
                UserTypeId = 1,
                StatusId = 1,
                IsEmailVerified = true,
                MustChangePassword = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static AgentEnrollmentToken AddToken(
            DLPSystemContext db,
            Guid createdByUserId,
            DateTimeOffset expiresAtUtc,
            int maxUses = 1,
            int usedCount = 0,
            DateTimeOffset? revokedAtUtc = null,
            string displayName = "Test Token")
        {
            var token = new AgentEnrollmentToken
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                TokenHash = Guid.NewGuid().ToString("N"),
                DisplayName = displayName,
                ExpiresAtUtc = expiresAtUtc,
                MaxUses = maxUses,
                UsedCount = usedCount,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RevokedAtUtc = revokedAtUtc
            };
            db.AgentEnrollmentTokens.Add(token);
            db.SaveChanges();
            return token;
        }

        [Fact]
        public async Task GetTokensAsync_NoStatusFilter_ReturnsEveryToken()
        {
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            AddToken(db, creator.Id, DateTimeOffset.UtcNow.AddDays(30), displayName: "Active one");
            AddToken(db, creator.Id, DateTimeOffset.UtcNow.AddDays(-1), displayName: "Expired one");
            var service = new EnrollmentTokenService(db);

            var result = await service.GetTokensAsync(TestDbContextFactory.OrganizationId, status: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.TotalCount);
        }

        [Theory]
        [InlineData("Active")]
        [InlineData("Expired")]
        [InlineData("Exhausted")]
        [InlineData("Revoked")]
        public async Task GetTokensAsync_StatusFilter_ReturnsOnlyTheMatchingToken(string status)
        {
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            var now = DateTimeOffset.UtcNow;

            var active = AddToken(db, creator.Id, now.AddDays(30), maxUses: 5, usedCount: 1, displayName: "Active");
            var expired = AddToken(db, creator.Id, now.AddDays(-1), displayName: "Expired");
            var exhausted = AddToken(db, creator.Id, now.AddDays(30), maxUses: 1, usedCount: 1, displayName: "Exhausted");
            var revoked = AddToken(db, creator.Id, now.AddDays(30), revokedAtUtc: now.AddMinutes(-5), displayName: "Revoked");

            var expectedId = status switch
            {
                "Active" => active.Id,
                "Expired" => expired.Id,
                "Exhausted" => exhausted.Id,
                "Revoked" => revoked.Id,
                _ => throw new InvalidOperationException()
            };

            var service = new EnrollmentTokenService(db);
            var result = await service.GetTokensAsync(TestDbContextFactory.OrganizationId, status, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(expectedId, item.Id);
        }

        [Fact]
        public async Task GetTokensAsync_RevokedTakesPrecedenceOverExpired()
        {
            // A token that is BOTH past its expiry AND revoked must classify as Revoked, not
            // Expired - same precedence EnrollmentTokensListComponent.status() applies (checks
            // revokedAtUtc first).
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            var now = DateTimeOffset.UtcNow;
            var token = AddToken(db, creator.Id, now.AddDays(-10), revokedAtUtc: now.AddDays(-5));

            var service = new EnrollmentTokenService(db);

            var revokedResult = await service.GetTokensAsync(TestDbContextFactory.OrganizationId, "Revoked", page: 1, pageSize: 20);
            var expiredResult = await service.GetTokensAsync(TestDbContextFactory.OrganizationId, "Expired", page: 1, pageSize: 20);

            Assert.Single(revokedResult.Data!.Items, x => x.Id == token.Id);
            Assert.DoesNotContain(expiredResult.Data!.Items, x => x.Id == token.Id);
        }

        [Fact]
        public async Task GetTokensAsync_UnknownStatusValue_ReturnsNoRows()
        {
            // Matches the existing PermissionGrantRuntimeStatus precedent: an unrecognized status
            // string is not an error, it just matches nothing (MatchesStatus is a plain equality
            // check against the computed classification).
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            AddToken(db, creator.Id, DateTimeOffset.UtcNow.AddDays(30));

            var service = new EnrollmentTokenService(db);
            var result = await service.GetTokensAsync(TestDbContextFactory.OrganizationId, "NotARealStatus", page: 1, pageSize: 20);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.TotalCount);
        }
    }

    public class AgentEnrollmentTokenRuntimeStatusTests
    {
        private static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Compute_NotExpiredNotExhaustedNotRevoked_IsActive()
        {
            var status = AgentEnrollmentTokenRuntimeStatus.Compute(
                revokedAtUtc: null, expiresAtUtc: NowUtc.AddDays(1), usedCount: 0, maxUses: 1, nowUtc: NowUtc);

            Assert.Equal("Active", status);
        }

        [Fact]
        public void Compute_PastExpiry_IsExpired()
        {
            var status = AgentEnrollmentTokenRuntimeStatus.Compute(
                revokedAtUtc: null, expiresAtUtc: NowUtc.AddSeconds(-1), usedCount: 0, maxUses: 1, nowUtc: NowUtc);

            Assert.Equal("Expired", status);
        }

        [Fact]
        public void Compute_UsedCountReachedMaxUses_IsExhausted()
        {
            var status = AgentEnrollmentTokenRuntimeStatus.Compute(
                revokedAtUtc: null, expiresAtUtc: NowUtc.AddDays(1), usedCount: 1, maxUses: 1, nowUtc: NowUtc);

            Assert.Equal("Exhausted", status);
        }

        [Fact]
        public void Compute_Revoked_TakesPrecedenceOverExpiredAndExhausted()
        {
            var status = AgentEnrollmentTokenRuntimeStatus.Compute(
                revokedAtUtc: NowUtc.AddMinutes(-1), expiresAtUtc: NowUtc.AddSeconds(-1), usedCount: 1, maxUses: 1, nowUtc: NowUtc);

            Assert.Equal("Revoked", status);
        }

        [Fact]
        public void Compute_ExpiredTakesPrecedenceOverExhausted()
        {
            var status = AgentEnrollmentTokenRuntimeStatus.Compute(
                revokedAtUtc: null, expiresAtUtc: NowUtc.AddSeconds(-1), usedCount: 1, maxUses: 1, nowUtc: NowUtc);

            Assert.Equal("Expired", status);
        }
    }
}
