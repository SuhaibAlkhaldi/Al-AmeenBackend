using DLPManagementSystem.DTO.Users;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the Admin-tab side of the account-device binding rule: every account (Admin or Employee
    // user type) now gets its own accompanying Employee row, a device is optional (unlike the Employee
    // tab, where it's mandatory) but validated the same way when supplied, the role-elevation guard is
    // generic/table-driven rather than a hardcoded HelpDesk-only branch, and a role that suggests
    // default permissions (SecurityAdmin/SuperAdmin) can have chosen ones granted at creation time -
    // see PROMPT context "Account-device binding rule".
    public class UserServiceTests
    {
        private static (DLPSystemContext Db, UserService Service) CreateService()
        {
            var db = TestDbContextFactory.Create();
            var passwordService = new PasswordService(new PasswordHasher<User>());
            var fakePolicyVersionService = new FakePolicyVersionService();
            var fakeAuditLogService = new FakeAdminAuditLogService();
            var deviceService = new DeviceService(db, fakePolicyVersionService, fakeAuditLogService);
            var lookupService = new PermissionLookupService(db, new MemoryCache(new MemoryCacheOptions()));
            var permissionGrantService = new PermissionGrantService(db, fakePolicyVersionService, lookupService, fakeAuditLogService);
            var service = new UserService(db, fakeAuditLogService, passwordService, deviceService, permissionGrantService, lookupService);
            return (db, service);
        }

        private const int SuperAdminRoleId = 1;
        private const int SecurityAdminRoleId = 2;
        private const int HelpDeskRoleId = 3;

        private static CreateUserDto ValidRequest(int roleId, Guid? deviceId = null, List<string>? suggestedKeys = null) => new()
        {
            FullName = "John Admin",
            Email = $"{Guid.NewGuid():N}@example.local",
            Password = "AdminChosenPassword1!",
            RoleId = roleId,
            UserTypeId = 1, // Admin, per TestDbContextFactory's seeded user types
            DeviceId = deviceId,
            SuggestedPermissionActionKeys = suggestedKeys
        };

        [Fact]
        public async Task CreateUserAsync_WithoutDevice_Succeeds_AndStillCreatesALinkedEmployeeRow()
        {
            var (db, service) = CreateService();

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "SuperAdmin", ValidRequest(SecurityAdminRoleId));

            Assert.True(result.Success);
            var employee = await db.Employees.SingleAsync(x => x.UserId == result.Data!.Id);
            Assert.Equal("John Admin", employee.DisplayName);
            Assert.Empty(await db.DeviceUserAssignments.ToListAsync());
        }

        [Fact]
        public async Task CreateUserAsync_HelpDeskAssigningSuperAdmin_IsRejected()
        {
            var (_, service) = CreateService();

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "HelpDesk", ValidRequest(SuperAdminRoleId));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateUserAsync_HelpDeskAssigningAuditor_IsAllowed()
        {
            var (_, service) = CreateService();
            const int auditorRoleId = 4;

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "HelpDesk", ValidRequest(auditorRoleId));

            Assert.True(result.Success);
        }

        [Fact]
        public async Task CreateUserAsync_SuperAdminAssigningAnyRole_IsUnrestricted()
        {
            var (_, service) = CreateService();

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "SuperAdmin", ValidRequest(SuperAdminRoleId));

            Assert.True(result.Success);
        }

        [Fact]
        public async Task CreateUserAsync_WithActiveDevice_LinksIt()
        {
            var (db, service) = CreateService();
            var device = TestDbContextFactory.AddDevice(db);

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "SuperAdmin", ValidRequest(SecurityAdminRoleId, device.Id));

            Assert.True(result.Success);
            var assignment = await db.DeviceUserAssignments.SingleAsync(x => x.DeviceId == device.Id && x.UnassignedAtUtc == null);
            var employee = await db.Employees.SingleAsync(x => x.UserId == result.Data!.Id);
            Assert.Equal(employee.Id, assignment.EmployeeId);
        }

        [Fact]
        public async Task CreateUserAsync_WithInactiveDevice_IsRejected()
        {
            var (db, service) = CreateService();
            var pendingDevice = TestDbContextFactory.AddDevice(db, deviceStatusId: 1); // "PendingEnrollment", not Active

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "SuperAdmin", ValidRequest(SecurityAdminRoleId, pendingDevice.Id));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateUserAsync_WithSelectedSuggestedPermission_CreatesAPermanentGrantForTheLinkedEmployee()
        {
            var (db, service) = CreateService();
            var device = TestDbContextFactory.AddDevice(db);

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "SuperAdmin",
                ValidRequest(SecurityAdminRoleId, device.Id, new List<string> { "usb.storage" }));

            Assert.True(result.Success);
            var employee = await db.Employees.SingleAsync(x => x.UserId == result.Data!.Id);
            var grant = await db.PermissionGrants.SingleAsync(x => x.SubjectId == employee.Id.ToString());
            Assert.Equal("usb.storage", grant.ActionKey);
            Assert.Null(grant.ExpiresAtUtc); // Permanent
            Assert.Null(grant.TargetDeviceId); // Employee-scoped, not tied to the specific device used at creation
        }

        [Fact]
        public async Task CreateUserAsync_WithoutSelectingAnySuggestedPermission_CreatesNoGrant()
        {
            var (db, service) = CreateService();
            var device = TestDbContextFactory.AddDevice(db);

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "SuperAdmin", ValidRequest(SecurityAdminRoleId, device.Id));

            Assert.True(result.Success);
            Assert.Empty(await db.PermissionGrants.ToListAsync());
        }

        [Fact]
        public async Task CreateUserAsync_WithUnknownSuggestedPermissionKey_IsRejected()
        {
            var (_, service) = CreateService();

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "SuperAdmin",
                ValidRequest(SecurityAdminRoleId, suggestedKeys: new List<string> { "not-a-real-action" }));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateUserAsync_WithDisabledSuggestedPermissionKey_IsRejected()
        {
            var (_, service) = CreateService();

            var result = await service.CreateUserAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), "SuperAdmin",
                ValidRequest(SecurityAdminRoleId, suggestedKeys: new List<string> { "disabled.action" }));

            Assert.False(result.Success);
        }
    }
}
