using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DLPManagementSystem.Tests.Service
{
    // Covers CreateDirectGrantAsync's new SubjectType support (Employee/Device/Organization - only
    // Employee existed before this feature) and the browser-extension get-or-create hookup shared
    // with PermissionRequestService (see PermissionRequestServiceTests for that path's own coverage).
    public class PermissionGrantServiceTests
    {
        private static User AddAdmin(DLPSystemContext db)
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

        private static Employee AddEmployee(DLPSystemContext db)
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
            return employee;
        }

        private static PermissionGrantService CreateService(DLPSystemContext db) => new(
            db,
            new FakePolicyVersionService(),
            new PermissionLookupService(db, new MemoryCache(new MemoryCacheOptions())),
            new FakeAdminAuditLogService());

        private static CreatePermissionGrantDto BuildGrant(string actionKey, string subjectType, string? subjectId) => new()
        {
            ActionKey = actionKey,
            SubjectType = subjectType,
            SubjectId = subjectId,
            GrantType = "Permanent",
            Reason = "Test grant."
        };

        [Fact]
        public async Task CreateDirectGrantAsync_DefaultEmployeeSubjectType_BehavesExactlyAsBefore()
        {
            var db = TestDbContextFactory.Create();
            var admin = AddAdmin(db);
            var employee = AddEmployee(db);
            var service = CreateService(db);

            // SubjectType omitted entirely - exercises the "Employee" default/back-compat path.
            var request = new CreatePermissionGrantDto
            {
                SubjectId = employee.Id.ToString(),
                ActionKey = "usb.storage",
                GrantType = "Permanent",
                Reason = "Regression check."
            };

            var result = await service.CreateDirectGrantAsync(TestDbContextFactory.OrganizationId, admin.Id, request);

            Assert.True(result.Success);
            Assert.Equal("Employee", result.Data!.SubjectType);
            Assert.Equal(employee.Id.ToString(), result.Data.SubjectId);
        }

        [Fact]
        public async Task CreateDirectGrantAsync_DeviceSubjectType_ResolvesAgainstDeviceTable()
        {
            var db = TestDbContextFactory.Create();
            var admin = AddAdmin(db);
            var device = TestDbContextFactory.AddDevice(db);
            var service = CreateService(db);

            var result = await service.CreateDirectGrantAsync(
                TestDbContextFactory.OrganizationId, admin.Id, BuildGrant("usb.storage", "Device", device.Id.ToString()));

            Assert.True(result.Success);
            Assert.Equal("Device", result.Data!.SubjectType);
            Assert.Equal(device.Id.ToString(), result.Data.SubjectId);
            Assert.Equal(device.Id, result.Data.TargetDeviceId);
        }

        [Fact]
        public async Task CreateDirectGrantAsync_OrganizationSubjectType_NeedsNoSubjectId()
        {
            var db = TestDbContextFactory.Create();
            var admin = AddAdmin(db);
            var service = CreateService(db);

            var result = await service.CreateDirectGrantAsync(
                TestDbContextFactory.OrganizationId, admin.Id, BuildGrant("usb.storage", "Organization", subjectId: null));

            Assert.True(result.Success);
            Assert.Equal("Organization", result.Data!.SubjectType);
            Assert.Equal(TestDbContextFactory.OrganizationId.ToString(), result.Data.SubjectId);
        }

        [Fact]
        public async Task CreateDirectGrantAsync_UnknownSubjectType_ReturnsFailure()
        {
            var db = TestDbContextFactory.Create();
            var admin = AddAdmin(db);
            var service = CreateService(db);

            var result = await service.CreateDirectGrantAsync(
                TestDbContextFactory.OrganizationId, admin.Id, BuildGrant("usb.storage", "DepartmentOfMagic", subjectId: null));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateDirectGrantAsync_NewExtensionIdViaDirectGrant_ProvisionsPermissionActionAndGrants()
        {
            var db = TestDbContextFactory.Create();
            var admin = AddAdmin(db);
            var device = TestDbContextFactory.AddDevice(db);
            var service = CreateService(db);
            const string extensionId = "bcdefghijklmnopabcdefghijklmnopa";

            var result = await service.CreateDirectGrantAsync(
                TestDbContextFactory.OrganizationId, admin.Id,
                BuildGrant("extension." + extensionId, "Device", device.Id.ToString()));

            Assert.True(result.Success);
            var action = await db.PermissionActions.FirstAsync(x => x.Key == "extension." + extensionId);
            Assert.True(action.IsEnabled);
        }
    }
}
