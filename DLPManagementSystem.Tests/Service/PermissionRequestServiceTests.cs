using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the uninstall/reinstall-adjacent feature: browser extension approval requests. An
    // extension id is arbitrary/unbounded (unlike every other, fixed-catalog ActionKey), so
    // PermissionRequestService.CreateAsync must lazily provision a PermissionAction row for a
    // never-before-seen "extension.<id>" key instead of rejecting it outright - while every other,
    // still-fixed action key keeps today's "must already exist" rejection unchanged (regression).
    public class PermissionRequestServiceTests
    {
        private static (User User, Employee Employee) AddEmployee(DLPSystemContext db)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                FullName = "Test Employee",
                Email = $"{Guid.NewGuid():N}@example.local",
                PasswordHash = "irrelevant-for-this-test",
                RoleId = 5,
                UserTypeId = 2,
                StatusId = 1,
                IsEmailVerified = true,
                MustChangePassword = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                UserId = user.Id,
                EmployeeNumber = Guid.NewGuid().ToString("N")[..10],
                DisplayName = "Test Employee",
                Email = user.Email,
                StatusId = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Employees.Add(employee);

            db.SaveChanges();
            return (user, employee);
        }

        private static PermissionRequestService CreateService(DLPSystemContext db) => new(
            db,
            new PermissionLookupService(db, new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())),
            new FakePolicyVersionService(),
            new PermissionGrantService(
                db,
                new FakePolicyVersionService(),
                new PermissionLookupService(db, new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())),
                new FakeAdminAuditLogService()),
            new FakeAdminAuditLogService());

        private static CreatePermissionRequestDto BuildExtensionRequest(string extensionId) => new()
        {
            ActionKey = "extension." + extensionId,
            GrantType = "Permanent",
            BusinessJustification = "Need this tool for daily work."
        };

        [Fact]
        public async Task CreateAsync_NewExtensionId_ProvisionsPermissionActionAndCreatesRequest()
        {
            var db = TestDbContextFactory.Create();
            var (user, _) = AddEmployee(db);
            var service = CreateService(db);
            const string extensionId = "abcdefghijklmnopabcdefghijklmnop";

            var result = await service.CreateAsync(TestDbContextFactory.OrganizationId, user.Id, 2, BuildExtensionRequest(extensionId));

            Assert.True(result.Success);

            var action = await db.PermissionActions.Include(x => x.Category).FirstAsync(x => x.Key == "extension." + extensionId);
            Assert.Equal("Extensions", action.Category.Name);
            Assert.True(action.IsEnabled);

            var request = await db.PermissionRequests.FirstAsync(x => x.ActionKey == "extension." + extensionId);
            Assert.Equal("Permanent", (await db.PermissionGrantTypes.FirstAsync(x => x.Id == request.RequestedGrantTypeId)).Name);
        }

        [Fact]
        public async Task CreateAsync_SecondRequestForSameExtensionId_ReusesExistingPermissionAction()
        {
            var db = TestDbContextFactory.Create();
            var (user, _) = AddEmployee(db);
            var service = CreateService(db);
            const string extensionId = "abcdefghijklmnopabcdefghijklmnop";

            await service.CreateAsync(TestDbContextFactory.OrganizationId, user.Id, 2, BuildExtensionRequest(extensionId));
            var firstActionRowId = (await db.PermissionActions.FirstAsync(x => x.Key == "extension." + extensionId)).Key;

            // A second employee requesting the same extension id must not fail and must not duplicate
            // the PermissionAction row (Key is its primary key - a duplicate insert would throw).
            var (secondUser, _) = AddEmployee(db);
            var secondResult = await service.CreateAsync(TestDbContextFactory.OrganizationId, secondUser.Id, 2, BuildExtensionRequest(extensionId));

            Assert.True(secondResult.Success);
            var matchingActions = await db.PermissionActions.Where(x => x.Key == "extension." + extensionId).ToListAsync();
            Assert.Single(matchingActions);
            Assert.Equal(firstActionRowId, matchingActions[0].Key);
        }

        [Fact]
        public async Task CreateAsync_InvalidExtensionIdShape_ReturnsFailure()
        {
            var db = TestDbContextFactory.Create();
            var (user, _) = AddEmployee(db);
            var service = CreateService(db);

            // Too short, and contains a digit - real Web Store ids are exactly 32 lowercase a-p letters.
            var result = await service.CreateAsync(TestDbContextFactory.OrganizationId, user.Id, 2, BuildExtensionRequest("not-a-real-id-123"));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateAsync_UnknownNonExtensionActionKey_StillReturnsFailure()
        {
            var db = TestDbContextFactory.Create();
            var (user, _) = AddEmployee(db);
            var service = CreateService(db);

            var result = await service.CreateAsync(TestDbContextFactory.OrganizationId, user.Id, 2, new CreatePermissionRequestDto
            {
                ActionKey = "some.unseeded.action",
                GrantType = "Permanent",
                BusinessJustification = "Need this tool for daily work."
            });

            Assert.False(result.Success);
        }
    }
}
