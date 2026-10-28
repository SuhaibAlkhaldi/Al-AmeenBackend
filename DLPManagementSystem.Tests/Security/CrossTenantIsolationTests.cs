using DLPManagementSystem.DTO.Alerts;
using DLPManagementSystem.DTO.Devices;
using DLPManagementSystem.DTO.Employees;
using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.DTO.Users;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DLPManagementSystem.Tests.Security
{
    // Cross-tenant regression pack: for every service below, a caller authenticated as org B must
    // never be able to read, modify, or even learn the existence of org A's rows by passing org A's
    // id - the kind of IDOR a second organization's SuperAdmin could otherwise use to see or tamper
    // with "Unlimited Innovation" (or vice versa). Every by-Id method is checked two ways: the call
    // must fail/not-found as org B, AND org A's row must be byte-for-byte unchanged afterward. Every
    // list method is checked by seeding one row in each org and confirming org B's list contains
    // exactly its own row, never org A's.
    //
    // Uses the same in-memory EF (TestDbContextFactory) + fake-service pattern as the existing
    // *ServiceTests files - no live SQL Server needed for anything covered here (see the bottom of
    // this file for what SQL-Server-only code this pack does NOT cover and why).
    public class CrossTenantIsolationTests
    {
        private static Guid AddOrganization(DLPSystemContext db, string code)
        {
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = code,
                Code = code,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Organizations.Add(org);
            db.SaveChanges();
            return org.Id;
        }

        private static User AddAdminUser(DLPSystemContext db, Guid organizationId, string label)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                FullName = label,
                Email = $"{Guid.NewGuid():N}@example.local",
                PasswordHash = "irrelevant-for-this-test",
                RoleId = 1, // SuperAdmin
                UserTypeId = 1, // Admin
                StatusId = 1,
                IsEmailVerified = true,
                MustChangePassword = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static (User User, Employee Employee) AddEmployee(DLPSystemContext db, Guid organizationId, string label)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                FullName = label,
                Email = $"{Guid.NewGuid():N}@example.local",
                PasswordHash = "irrelevant-for-this-test",
                RoleId = 5, // Employee
                UserTypeId = 2, // Employee
                StatusId = 1,
                IsEmailVerified = true,
                MustChangePassword = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = user.Id,
                EmployeeNumber = Guid.NewGuid().ToString("N")[..10],
                DisplayName = label,
                Email = user.Email,
                StatusId = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Employees.Add(employee);
            db.SaveChanges();
            return (user, employee);
        }

        // Alert-related lookups are global catalogs (not org-scoped) - seed once per db instance, reused
        // for both orgs, same as production. Checked against THIS db only (TestDbContextFactory.Create()
        // builds a fresh in-memory database per test) - a static/shared guard here would wrongly skip
        // seeding on a later test's brand-new db.
        private static void EnsureAlertLookupsSeeded(DLPSystemContext db)
        {
            if (db.AlertLevels.Any())
            {
                return;
            }
            db.AlertLevels.Add(new AlertLevel { Id = 1, Name = "Medium", MinRiskScore = 40, MaxRiskScore = 69 });
            db.AlertStatuses.Add(new AlertStatus { Id = 1, Name = "New" });
            db.AuditEventTypes.Add(new AuditEventType { Id = 1, Name = "PermissionCheck", DisplayName = "Permission Check" });
            db.AuditDecisions.Add(new AuditDecision { Id = 1, Name = "Block", DisplayName = "Block" });
            db.SaveChanges();
        }

        private static Alert AddAlert(DLPSystemContext db, Guid organizationId, Device device)
        {
            EnsureAlertLookupsSeeded(db);

            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DeviceId = device.Id,
                ActionKey = "usb.storage",
                EventTypeId = 1,
                DecisionId = 1,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                ReceivedAtUtc = DateTimeOffset.UtcNow
            };
            db.AuditEvents.Add(auditEvent);

            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AuditEventId = auditEvent.Id,
                AlertLevelId = 1,
                AlertStatusId = 1,
                Title = "Test alert",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Alerts.Add(alert);
            db.SaveChanges();
            return alert;
        }

        private static PermissionGrant AddPermissionGrant(DLPSystemContext db, Guid organizationId, Guid employeeId, Guid grantedByUserId)
        {
            var grant = new PermissionGrant
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ActionKey = "usb.storage",
                DecisionId = 1, // Allow
                SubjectTypeId = 3, // Employee
                SubjectId = employeeId.ToString(),
                GrantTypeId = 1, // Permanent
                Priority = 0,
                StartsAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                Reason = "Seed grant for cross-tenant test.",
                GrantedByUserId = grantedByUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.PermissionGrants.Add(grant);
            db.SaveChanges();
            return grant;
        }

        private static PermissionRequest AddPermissionRequest(DLPSystemContext db, Guid organizationId, Guid requestedByUserId, Guid requestedByEmployeeId)
        {
            var request = new PermissionRequest
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RequestedByUserId = requestedByUserId,
                RequestedByEmployeeId = requestedByEmployeeId,
                ActionKey = "usb.storage",
                RequestedDecisionId = 1, // Allow
                RequestedGrantTypeId = 1, // Permanent
                SubjectTypeId = 3, // Employee
                SubjectId = requestedByEmployeeId.ToString(),
                BusinessJustification = "Need this for work.",
                StatusId = 2, // Submitted
                SubmittedAtUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.PermissionRequests.Add(request);
            db.SaveChanges();
            return request;
        }

        private static AgentEnrollmentToken AddEnrollmentToken(DLPSystemContext db, Guid organizationId, Guid createdByUserId)
        {
            var token = new AgentEnrollmentToken
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TokenHash = Guid.NewGuid().ToString("N"),
                DisplayName = "Test token",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
                MaxUses = 10,
                UsedCount = 0,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RevokedAtUtc = null
            };
            db.AgentEnrollmentTokens.Add(token);
            db.SaveChanges();
            return token;
        }

        // ---------------------------------------------------------------------------------------
        // DeviceService
        // ---------------------------------------------------------------------------------------

        private static DeviceService CreateDeviceService(DLPSystemContext db) =>
            new(db, new FakePolicyVersionService(), new FakeAdminAuditLogService());

        [Fact]
        public async Task DeviceService_GetByIdAsync_AsOrgB_OrgADevice_IsNotFound()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-DEV-GET");
            var orgB = AddOrganization(db, "ORGB-DEV-GET");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var service = CreateDeviceService(db);

            var result = await service.GetDeviceByIdAsync(orgB, deviceA.Id);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task DeviceService_UpdateDeviceAsync_AsOrgB_OrgADevice_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-DEV-UPD");
            var orgB = AddOrganization(db, "ORGB-DEV-UPD");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var originalName = deviceA.MachineName;
            var originalStatusId = deviceA.StatusId;
            var service = CreateDeviceService(db);

            var result = await service.UpdateDeviceAsync(
                orgB, deviceA.Id, adminB.Id, new UpdateDeviceDto { MachineName = "HIJACKED", StatusId = originalStatusId });

            Assert.False(result.Success);
            var reloaded = await db.Devices.AsNoTracking().SingleAsync(x => x.Id == deviceA.Id);
            Assert.Equal(originalName, reloaded.MachineName);
            Assert.Equal(originalStatusId, reloaded.StatusId);
        }

        [Fact]
        public async Task DeviceService_DeleteDeviceAsync_AsOrgB_OrgADevice_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-DEV-DEL");
            var orgB = AddOrganization(db, "ORGB-DEV-DEL");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var originalStatusId = deviceA.StatusId;
            var service = CreateDeviceService(db);

            var result = await service.DeleteDeviceAsync(orgB, deviceA.Id, adminB.Id);

            Assert.False(result.Success);
            var reloaded = await db.Devices.AsNoTracking().SingleAsync(x => x.Id == deviceA.Id);
            Assert.Equal(originalStatusId, reloaded.StatusId);
        }

        [Fact]
        public async Task DeviceService_AssignDeviceAsync_AsOrgB_OrgADevice_FailsAndCreatesNoAssignment()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-DEV-ASG");
            var orgB = AddOrganization(db, "ORGB-DEV-ASG");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var (_, employeeB) = AddEmployee(db, orgB, "Employee B");
            var service = CreateDeviceService(db);

            // Even an employee that genuinely exists (in org B) must not get attached to org A's device.
            var result = await service.AssignDeviceAsync(
                orgB, deviceA.Id, adminB.Id, new AssignDeviceDto { EmployeeId = employeeB.Id });

            Assert.False(result.Success);
            Assert.False(await db.DeviceUserAssignments.AnyAsync(x => x.DeviceId == deviceA.Id));
        }

        [Fact]
        public async Task DeviceService_GetDevicesAsync_AsOrgB_ReturnsOnlyOrgBDevices()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-DEV-LIST");
            var orgB = AddOrganization(db, "ORGB-DEV-LIST");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var deviceB = TestDbContextFactory.AddDevice(db, organizationId: orgB);
            var service = CreateDeviceService(db);

            var result = await service.GetDevicesAsync(orgB, search: null, statusId: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(deviceB.Id, item.Id);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == deviceA.Id);
        }

        // ---------------------------------------------------------------------------------------
        // EmployeeService
        // ---------------------------------------------------------------------------------------

        private static EmployeeService CreateEmployeeService(DLPSystemContext db) =>
            new(db, new FakeAdminAuditLogService(), new PasswordService(new PasswordHasher<User>()),
                CreateDeviceService(db));

        [Fact]
        public async Task EmployeeService_GetByIdAsync_AsOrgB_OrgAEmployee_IsNotFound()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-EMP-GET");
            var orgB = AddOrganization(db, "ORGB-EMP-GET");
            var (_, employeeA) = AddEmployee(db, orgA, "Employee A");
            var service = CreateEmployeeService(db);

            var result = await service.GetEmployeeByIdAsync(orgB, employeeA.Id);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task EmployeeService_UpdateEmployeeAsync_AsOrgB_OrgAEmployee_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-EMP-UPD");
            var orgB = AddOrganization(db, "ORGB-EMP-UPD");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (_, employeeA) = AddEmployee(db, orgA, "Employee A");
            var originalName = employeeA.DisplayName;
            var service = CreateEmployeeService(db);

            var result = await service.UpdateEmployeeAsync(orgB, employeeA.Id, adminB.Id, new UpdateEmployeeDto
            {
                EmployeeNumber = employeeA.EmployeeNumber,
                DisplayName = "HIJACKED",
                Email = employeeA.Email,
                StatusId = employeeA.StatusId
            });

            Assert.False(result.Success);
            var reloaded = await db.Employees.AsNoTracking().SingleAsync(x => x.Id == employeeA.Id);
            Assert.Equal(originalName, reloaded.DisplayName);
        }

        [Fact]
        public async Task EmployeeService_DeleteEmployeeAsync_AsOrgB_OrgAEmployee_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-EMP-DEL");
            var orgB = AddOrganization(db, "ORGB-EMP-DEL");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (_, employeeA) = AddEmployee(db, orgA, "Employee A");
            var originalStatusId = employeeA.StatusId;
            var service = CreateEmployeeService(db);

            var result = await service.DeleteEmployeeAsync(orgB, employeeA.Id, adminB.Id);

            Assert.False(result.Success);
            var reloaded = await db.Employees.AsNoTracking().SingleAsync(x => x.Id == employeeA.Id);
            Assert.Equal(originalStatusId, reloaded.StatusId);
        }

        [Fact]
        public async Task EmployeeService_GetEmployeesAsync_AsOrgB_ReturnsOnlyOrgBEmployees()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-EMP-LIST");
            var orgB = AddOrganization(db, "ORGB-EMP-LIST");
            var (_, employeeA) = AddEmployee(db, orgA, "Employee A");
            var (_, employeeB) = AddEmployee(db, orgB, "Employee B");
            var service = CreateEmployeeService(db);

            var result = await service.GetEmployeesAsync(orgB, search: null, departmentId: null, statusId: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(employeeB.Id, item.Id);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == employeeA.Id);
        }

        // ---------------------------------------------------------------------------------------
        // UserService
        // ---------------------------------------------------------------------------------------

        private static UserService CreateUserService(DLPSystemContext db)
        {
            var lookupService = new PermissionLookupService(db, new MemoryCache(new MemoryCacheOptions()));
            var permissionGrantService = new PermissionGrantService(
                db, new FakePolicyVersionService(), lookupService, new FakeAdminAuditLogService());
            return new UserService(
                db, new FakeAdminAuditLogService(), new PasswordService(new PasswordHasher<User>()),
                CreateDeviceService(db), permissionGrantService, lookupService);
        }

        [Fact]
        public async Task UserService_GetByIdAsync_AsOrgB_OrgAUser_IsNotFound()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-USR-GET");
            var orgB = AddOrganization(db, "ORGB-USR-GET");
            var userA = AddAdminUser(db, orgA, "User A");
            var service = CreateUserService(db);

            var result = await service.GetUserByIdAsync(orgB, userA.Id);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task UserService_UpdateUserAsync_AsOrgB_OrgAUser_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-USR-UPD");
            var orgB = AddOrganization(db, "ORGB-USR-UPD");
            var userA = AddAdminUser(db, orgA, "User A");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var originalName = userA.FullName;
            var originalRoleId = userA.RoleId;
            var service = CreateUserService(db);

            var result = await service.UpdateUserAsync(orgB, userA.Id, adminB.Id, "SuperAdmin",
                new UpdateUserDto { FullName = "HIJACKED", RoleId = originalRoleId, StatusId = userA.StatusId });

            Assert.False(result.Success);
            var reloaded = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userA.Id);
            Assert.Equal(originalName, reloaded.FullName);
            Assert.Equal(originalRoleId, reloaded.RoleId);
        }

        [Fact]
        public async Task UserService_ResetPasswordAsync_AsOrgB_OrgAUser_FailsAndLeavesHashUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-USR-PWD");
            var orgB = AddOrganization(db, "ORGB-USR-PWD");
            var userA = AddAdminUser(db, orgA, "User A");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var originalHash = userA.PasswordHash;
            var service = CreateUserService(db);

            var result = await service.ResetPasswordAsync(orgB, userA.Id, adminB.Id,
                new ResetPasswordDto { NewPassword = "BrandNewPassword1!" });

            Assert.False(result.Success);
            var reloaded = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userA.Id);
            Assert.Equal(originalHash, reloaded.PasswordHash);
        }

        [Fact]
        public async Task UserService_DeleteUserAsync_AsOrgB_OrgAUser_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-USR-DEL");
            var orgB = AddOrganization(db, "ORGB-USR-DEL");
            var userA = AddAdminUser(db, orgA, "User A");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var originalStatusId = userA.StatusId;
            var service = CreateUserService(db);

            var result = await service.DeleteUserAsync(orgB, userA.Id, adminB.Id);

            Assert.False(result.Success);
            var reloaded = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userA.Id);
            Assert.Equal(originalStatusId, reloaded.StatusId);
        }

        [Fact]
        public async Task UserService_GetUsersAsync_AsOrgB_ReturnsOnlyOrgBUsers()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-USR-LIST");
            var orgB = AddOrganization(db, "ORGB-USR-LIST");
            var userA = AddAdminUser(db, orgA, "User A");
            var userB = AddAdminUser(db, orgB, "User B");
            var service = CreateUserService(db);

            var result = await service.GetUsersAsync(orgB, search: null, roleId: null, statusId: null, userTypeId: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(userB.Id, item.Id);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == userA.Id);
        }

        // ---------------------------------------------------------------------------------------
        // AlertService
        // ---------------------------------------------------------------------------------------

        private static AlertService CreateAlertService(DLPSystemContext db) => new(db);

        [Fact]
        public async Task AlertService_GetByIdAsync_AsOrgB_OrgAAlert_IsNotFound()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-ALR-GET");
            var orgB = AddOrganization(db, "ORGB-ALR-GET");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var alertA = AddAlert(db, orgA, deviceA);
            var service = CreateAlertService(db);

            var result = await service.GetAlertByIdAsync(orgB, alertA.Id);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task AlertService_AssignAlertAsync_AsOrgB_OrgAAlert_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-ALR-ASG");
            var orgB = AddOrganization(db, "ORGB-ALR-ASG");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var alertA = AddAlert(db, orgA, deviceA);
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var originalAssignee = alertA.AssignedToUserId;
            var service = CreateAlertService(db);

            var result = await service.AssignAlertAsync(orgB, alertA.Id, new AssignAlertDto { AssignedToUserId = adminB.Id });

            Assert.False(result.Success);
            var reloaded = await db.Alerts.AsNoTracking().SingleAsync(x => x.Id == alertA.Id);
            Assert.Equal(originalAssignee, reloaded.AssignedToUserId);
        }

        [Fact]
        public async Task AlertService_UpdateAlertStatusAsync_AsOrgB_OrgAAlert_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-ALR-STA");
            var orgB = AddOrganization(db, "ORGB-ALR-STA");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var alertA = AddAlert(db, orgA, deviceA);
            var originalStatusId = alertA.AlertStatusId;
            var service = CreateAlertService(db);

            var result = await service.UpdateAlertStatusAsync(orgB, alertA.Id, new UpdateAlertStatusDto { AlertStatusId = originalStatusId });

            Assert.False(result.Success);
            var reloaded = await db.Alerts.AsNoTracking().SingleAsync(x => x.Id == alertA.Id);
            Assert.Equal(originalStatusId, reloaded.AlertStatusId);
        }

        [Fact]
        public async Task AlertService_GetAlertsAsync_AsOrgB_ReturnsOnlyOrgBAlerts()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-ALR-LIST");
            var orgB = AddOrganization(db, "ORGB-ALR-LIST");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var deviceB = TestDbContextFactory.AddDevice(db, organizationId: orgB);
            var alertA = AddAlert(db, orgA, deviceA);
            var alertB = AddAlert(db, orgB, deviceB);
            var service = CreateAlertService(db);

            var result = await service.GetAlertsAsync(orgB, statusId: null, levelId: null, assignedToUserId: null, fromUtc: null, toUtc: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(alertB.Id, item.Id);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == alertA.Id);
        }

        // ---------------------------------------------------------------------------------------
        // AuditEventService (list only - no by-Id endpoint exists on this controller)
        // ---------------------------------------------------------------------------------------

        [Fact]
        public async Task AuditEventService_GetAuditEventsAsync_AsOrgB_ReturnsOnlyOrgBEvents()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-AUD-LIST");
            var orgB = AddOrganization(db, "ORGB-AUD-LIST");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var deviceB = TestDbContextFactory.AddDevice(db, organizationId: orgB);
            var eventA = new AuditEvent
            {
                Id = Guid.NewGuid(), OrganizationId = orgA, DeviceId = deviceA.Id, ActionKey = "usb.storage",
                EventTypeId = 1, DecisionId = 1, OccurredAtUtc = DateTimeOffset.UtcNow, ReceivedAtUtc = DateTimeOffset.UtcNow
            };
            var eventB = new AuditEvent
            {
                Id = Guid.NewGuid(), OrganizationId = orgB, DeviceId = deviceB.Id, ActionKey = "usb.storage",
                EventTypeId = 1, DecisionId = 1, OccurredAtUtc = DateTimeOffset.UtcNow, ReceivedAtUtc = DateTimeOffset.UtcNow
            };
            EnsureAlertLookupsSeeded(db); // provides AuditEventTypes/AuditDecisions Id=1
            db.AuditEvents.AddRange(eventA, eventB);
            db.SaveChanges();
            var service = new AuditEventService(db);

            var result = await service.GetAuditEventsAsync(orgB, deviceId: null, employeeId: null, actionKey: null,
                decisionId: null, reasonCodeId: null, fromUtc: null, toUtc: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(eventB.Id, item.Id);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == eventA.Id);
        }

        // ---------------------------------------------------------------------------------------
        // PermissionGrantService
        // ---------------------------------------------------------------------------------------

        private static PermissionGrantService CreatePermissionGrantService(DLPSystemContext db) => new(
            db, new FakePolicyVersionService(), new PermissionLookupService(db, new MemoryCache(new MemoryCacheOptions())),
            new FakeAdminAuditLogService());

        [Fact]
        public async Task PermissionGrantService_RevokeAsync_AsOrgB_OrgAGrant_FailsAndLeavesRowUnrevoked()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-GRT-REV");
            var orgB = AddOrganization(db, "ORGB-GRT-REV");
            var adminA = AddAdminUser(db, orgA, "Admin A");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (_, employeeA) = AddEmployee(db, orgA, "Employee A");
            var grantA = AddPermissionGrant(db, orgA, employeeA.Id, adminA.Id);
            var service = CreatePermissionGrantService(db);

            var result = await service.RevokeAsync(orgB, grantA.Id, adminB.Id, new RevokePermissionGrantDto { RevocationReason = "Hijack attempt." });

            Assert.False(result.Success);
            var reloaded = await db.PermissionGrants.AsNoTracking().SingleAsync(x => x.Id == grantA.Id);
            Assert.Null(reloaded.RevokedAtUtc);
        }

        [Fact]
        public async Task PermissionGrantService_CreateDirectGrantAsync_AsOrgB_EmployeeSubjectIdFromOrgA_Fails()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-GRT-CRE-EMP");
            var orgB = AddOrganization(db, "ORGB-GRT-CRE-EMP");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (_, employeeA) = AddEmployee(db, orgA, "Employee A");
            var service = CreatePermissionGrantService(db);

            var result = await service.CreateDirectGrantAsync(orgB, adminB.Id, new CreatePermissionGrantDto
            {
                ActionKey = "usb.storage",
                SubjectType = "Employee",
                SubjectId = employeeA.Id.ToString(),
                GrantType = "Permanent",
                Reason = "Cross-tenant reference injection attempt."
            });

            Assert.False(result.Success);
            Assert.False(await db.PermissionGrants.AnyAsync(x => x.SubjectId == employeeA.Id.ToString()));
        }

        [Fact]
        public async Task PermissionGrantService_CreateDirectGrantAsync_AsOrgB_DeviceSubjectIdFromOrgA_Fails()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-GRT-CRE-DEV");
            var orgB = AddOrganization(db, "ORGB-GRT-CRE-DEV");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var deviceA = TestDbContextFactory.AddDevice(db, organizationId: orgA);
            var service = CreatePermissionGrantService(db);

            var result = await service.CreateDirectGrantAsync(orgB, adminB.Id, new CreatePermissionGrantDto
            {
                ActionKey = "usb.storage",
                SubjectType = "Device",
                SubjectId = deviceA.Id.ToString(),
                GrantType = "Permanent",
                Reason = "Cross-tenant reference injection attempt."
            });

            Assert.False(result.Success);
            Assert.False(await db.PermissionGrants.AnyAsync(x => x.SubjectId == deviceA.Id.ToString()));
        }

        [Fact]
        public async Task PermissionGrantService_GetGrantsAsync_AsOrgB_ReturnsOnlyOrgBGrants()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-GRT-LIST");
            var orgB = AddOrganization(db, "ORGB-GRT-LIST");
            var adminA = AddAdminUser(db, orgA, "Admin A");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (_, employeeA) = AddEmployee(db, orgA, "Employee A");
            var (_, employeeB) = AddEmployee(db, orgB, "Employee B");
            var grantA = AddPermissionGrant(db, orgA, employeeA.Id, adminA.Id);
            var grantB = AddPermissionGrant(db, orgB, employeeB.Id, adminB.Id);
            var service = CreatePermissionGrantService(db);

            var result = await service.GetGrantsAsync(orgB, adminB.Id, callerUserTypeId: 1, subjectId: null, actionKey: null, status: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(grantB.Id, item.Id);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == grantA.Id);
        }

        // ---------------------------------------------------------------------------------------
        // PermissionRequestService
        // ---------------------------------------------------------------------------------------

        private static PermissionRequestService CreatePermissionRequestService(DLPSystemContext db)
        {
            var lookupService = new PermissionLookupService(db, new MemoryCache(new MemoryCacheOptions()));
            return new PermissionRequestService(
                db, lookupService, new FakePolicyVersionService(),
                new PermissionGrantService(db, new FakePolicyVersionService(), lookupService, new FakeAdminAuditLogService()),
                new FakeAdminAuditLogService());
        }

        [Fact]
        public async Task PermissionRequestService_GetByIdAsync_AsOrgB_OrgARequest_IsNotFound()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-REQ-GET");
            var orgB = AddOrganization(db, "ORGB-REQ-GET");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (userA, employeeA) = AddEmployee(db, orgA, "Employee A");
            var requestA = AddPermissionRequest(db, orgA, userA.Id, employeeA.Id);
            var service = CreatePermissionRequestService(db);

            var result = await service.GetByIdAsync(orgB, requestA.Id, adminB.Id, callerUserTypeId: 1);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task PermissionRequestService_ApproveAsync_AsOrgB_OrgARequest_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-REQ-APR");
            var orgB = AddOrganization(db, "ORGB-REQ-APR");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (userA, employeeA) = AddEmployee(db, orgA, "Employee A");
            var requestA = AddPermissionRequest(db, orgA, userA.Id, employeeA.Id);
            var originalStatusId = requestA.StatusId;
            var service = CreatePermissionRequestService(db);

            var result = await service.ApproveAsync(orgB, requestA.Id, adminB.Id, callerUserTypeId: 1, new ReviewPermissionRequestDto());

            Assert.False(result.Success);
            var reloaded = await db.PermissionRequests.AsNoTracking().SingleAsync(x => x.Id == requestA.Id);
            Assert.Equal(originalStatusId, reloaded.StatusId);
            Assert.Null(reloaded.ReviewedAtUtc);
        }

        [Fact]
        public async Task PermissionRequestService_RejectAsync_AsOrgB_OrgARequest_FailsAndLeavesRowUnchanged()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-REQ-REJ");
            var orgB = AddOrganization(db, "ORGB-REQ-REJ");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (userA, employeeA) = AddEmployee(db, orgA, "Employee A");
            var requestA = AddPermissionRequest(db, orgA, userA.Id, employeeA.Id);
            var originalStatusId = requestA.StatusId;
            var service = CreatePermissionRequestService(db);

            var result = await service.RejectAsync(orgB, requestA.Id, adminB.Id, callerUserTypeId: 1, new ReviewPermissionRequestDto());

            Assert.False(result.Success);
            var reloaded = await db.PermissionRequests.AsNoTracking().SingleAsync(x => x.Id == requestA.Id);
            Assert.Equal(originalStatusId, reloaded.StatusId);
            Assert.Null(reloaded.ReviewedAtUtc);
        }

        [Fact]
        public async Task PermissionRequestService_GetRequestsAsync_AsOrgB_ReturnsOnlyOrgBRequests()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-REQ-LIST");
            var orgB = AddOrganization(db, "ORGB-REQ-LIST");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var (userA, employeeA) = AddEmployee(db, orgA, "Employee A");
            var (userB, employeeB) = AddEmployee(db, orgB, "Employee B");
            var requestA = AddPermissionRequest(db, orgA, userA.Id, employeeA.Id);
            var requestB = AddPermissionRequest(db, orgB, userB.Id, employeeB.Id);
            var service = CreatePermissionRequestService(db);

            var result = await service.GetRequestsAsync(orgB, adminB.Id, callerUserTypeId: 1, statusId: null, requestedByEmployeeId: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(requestB.Id, item.Id);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == requestA.Id);
        }

        // ---------------------------------------------------------------------------------------
        // EnrollmentTokenService
        // ---------------------------------------------------------------------------------------

        [Fact]
        public async Task EnrollmentTokenService_RevokeTokenAsync_AsOrgB_OrgAToken_FailsAndLeavesRowUnrevoked()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-TOK-REV");
            var orgB = AddOrganization(db, "ORGB-TOK-REV");
            var adminA = AddAdminUser(db, orgA, "Admin A");
            var tokenA = AddEnrollmentToken(db, orgA, adminA.Id);
            var service = new EnrollmentTokenService(db);

            var result = await service.RevokeTokenAsync(orgB, tokenA.Id);

            Assert.False(result.Success);
            var reloaded = await db.AgentEnrollmentTokens.AsNoTracking().SingleAsync(x => x.Id == tokenA.Id);
            Assert.Null(reloaded.RevokedAtUtc);
        }

        [Fact]
        public async Task EnrollmentTokenService_GetTokensAsync_AsOrgB_ReturnsOnlyOrgBTokens()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-TOK-LIST");
            var orgB = AddOrganization(db, "ORGB-TOK-LIST");
            var adminA = AddAdminUser(db, orgA, "Admin A");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var tokenA = AddEnrollmentToken(db, orgA, adminA.Id);
            var tokenB = AddEnrollmentToken(db, orgB, adminB.Id);
            var service = new EnrollmentTokenService(db);

            var result = await service.GetTokensAsync(orgB, status: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal(tokenB.Id, item.Id);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == tokenA.Id);
        }

        // ---------------------------------------------------------------------------------------
        // AdminAuditLogService
        // ---------------------------------------------------------------------------------------

        [Fact]
        public async Task AdminAuditLogService_GetAdminAuditLogsAsync_AsOrgB_ReturnsOnlyOrgBLogs()
        {
            var db = TestDbContextFactory.Create();
            var orgA = AddOrganization(db, "ORGA-AAL-LIST");
            var orgB = AddOrganization(db, "ORGB-AAL-LIST");
            var adminA = AddAdminUser(db, orgA, "Admin A");
            var adminB = AddAdminUser(db, orgB, "Admin B");
            var service = new AdminAuditLogService(db);

            await service.LogAsync(orgA, adminA.Id, "DeviceStatusChanged", "Device", Guid.NewGuid(), "Device A", null);
            await service.LogAsync(orgB, adminB.Id, "DeviceStatusChanged", "Device", Guid.NewGuid(), "Device B", null);
            await db.SaveChangesAsync();

            var result = await service.GetAdminAuditLogsAsync(orgB, actorUserId: null, actionType: null, targetType: null,
                fromUtc: null, toUtc: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal("Device B", item.TargetDisplayName);
        }

        // ---------------------------------------------------------------------------------------
        // Not covered here - requires a live SQL Server, not the EF InMemory provider:
        //   - SqlDlpDashboardQueryService / RollupDlpDashboardQueryService (raw ADO.NET SqlCommand
        //     against a real SqlConnection - these classes don't go through DbContext at all, so
        //     InMemory can't exercise them; see SqlDlpDashboardQueryServiceTests, which already
        //     requires a real local SQL Server and is known to time out in this sandbox).
        //   - DashboardRollupAdminController's organization-scoping of DashboardRollupWatermarks and
        //     the backfill/refresh services, for the same reason.
        //   - AuditRetentionWorker's per-organization archive/purge grouping - out of this pack's
        //     requested scope (services, not background workers); reviewed by direct code read in the
        //     prior audit and confirmed there to group correctly by OrganizationId. It IS DbContext-based
        //     (unlike the two raw-ADO.NET items above) so an InMemory test for it is possible later.
        // ---------------------------------------------------------------------------------------
    }
}
