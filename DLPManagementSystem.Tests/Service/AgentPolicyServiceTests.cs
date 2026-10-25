using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;

namespace DLPManagementSystem.Tests.Service
{
    // Closes a pre-existing latent gap confirmed live while building the browser-extension approval
    // feature: BuildGrantsForDeviceAsync only ever resolved Employee-subject grants. Organization- and
    // Device-subject grants already existed in the schema and already showed up fine in the admin
    // portal, but never reached any agent. These tests cover the fix directly through the public
    // GetPolicyAsync entry point (BuildGrantsForDeviceAsync itself is private).
    public class AgentPolicyServiceTests
    {
        private static User AddGrantor(DLPSystemContext db)
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

        private static void AddPolicyVersion(DLPSystemContext db, long versionNumber = 1)
        {
            db.PolicyVersions.Add(new PolicyVersion
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                VersionNumber = versionNumber,
                ChangedAtUtc = DateTimeOffset.UtcNow,
                ChangeReason = "Test"
            });
            db.SaveChanges();
        }

        private static PermissionGrant AddGrant(
            DLPSystemContext db, Guid grantedByUserId, string actionKey, string subjectTypeName, string subjectId, Guid? targetDeviceId = null)
        {
            var subjectTypeId = subjectTypeName switch
            {
                "Employee" => 3,
                "Device" => 5,
                "Organization" => 1,
                _ => throw new InvalidOperationException()
            };

            var grant = new PermissionGrant
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                ActionKey = actionKey,
                DecisionId = 1, // Allow
                SubjectTypeId = subjectTypeId,
                SubjectId = subjectId,
                TargetDeviceId = targetDeviceId,
                GrantTypeId = 1, // Permanent
                Priority = 500,
                StartsAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresAtUtc = null,
                Reason = "Test",
                GrantedByUserId = grantedByUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.PermissionGrants.Add(grant);
            db.SaveChanges();
            return grant;
        }

        private static AgentPolicyService CreateService(DLPSystemContext db) =>
            new(db, new FakePolicySigningService());

        [Fact]
        public async Task GetPolicyAsync_OrganizationScopedGrant_ReachesAnyDeviceInThatOrganization()
        {
            var db = TestDbContextFactory.Create();
            var admin = AddGrantor(db);
            AddPolicyVersion(db);
            var device = TestDbContextFactory.AddDevice(db);
            AddGrant(db, admin.Id, "extension.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Organization", TestDbContextFactory.OrganizationId.ToString());

            var service = CreateService(db);
            var result = await service.GetPolicyAsync(TestDbContextFactory.OrganizationId, device.Id, currentVersion: 0, userSid: null);

            Assert.True(result.Success);
            Assert.True(result.Data!.HasUpdate);
            var grants = result.Data.Snapshot!.Policy.Permissions.Grants;
            Assert.Contains(grants, g => g.ActionKey == "extension.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" && g.Allowed);
        }

        [Fact]
        public async Task GetPolicyAsync_DeviceScopedGrant_ReachesOnlyThatDevice()
        {
            var db = TestDbContextFactory.Create();
            var admin = AddGrantor(db);
            AddPolicyVersion(db);
            var deviceA = TestDbContextFactory.AddDevice(db);
            var deviceB = TestDbContextFactory.AddDevice(db);
            AddGrant(db, admin.Id, "extension.unrestricted", "Device", deviceA.Id.ToString(), targetDeviceId: deviceA.Id);

            var service = CreateService(db);

            var resultA = await service.GetPolicyAsync(TestDbContextFactory.OrganizationId, deviceA.Id, currentVersion: 0, userSid: null);
            var resultB = await service.GetPolicyAsync(TestDbContextFactory.OrganizationId, deviceB.Id, currentVersion: 0, userSid: null);

            Assert.Contains(resultA.Data!.Snapshot!.Policy.Permissions.Grants, g => g.ActionKey == "extension.unrestricted");
            Assert.DoesNotContain(resultB.Data!.Snapshot!.Policy.Permissions.Grants, g => g.ActionKey == "extension.unrestricted");
        }

        [Fact]
        public async Task GetPolicyAsync_DeviceScopedGrant_ReachesDeviceWithNoAssignedEmployee()
        {
            // Regression guard for the fix's own subtlety: the old code's early "no employee resolved
            // -> return empty" path must not suppress Organization/Device grants for an unassigned device.
            var db = TestDbContextFactory.Create();
            var admin = AddGrantor(db);
            AddPolicyVersion(db);
            var device = TestDbContextFactory.AddDevice(db); // no DeviceUserAssignment at all
            AddGrant(db, admin.Id, "extension.unrestricted", "Device", device.Id.ToString(), targetDeviceId: device.Id);

            var service = CreateService(db);
            var result = await service.GetPolicyAsync(TestDbContextFactory.OrganizationId, device.Id, currentVersion: 0, userSid: null);

            Assert.Contains(result.Data!.Snapshot!.Policy.Permissions.Grants, g => g.ActionKey == "extension.unrestricted");
        }

        [Fact]
        public async Task GetPolicyAsync_EmployeeScopedGrantWithTargetDevice_BehavesExactlyAsBeforeThisFix()
        {
            var db = TestDbContextFactory.Create();
            var admin = AddGrantor(db);
            AddPolicyVersion(db);
            var device = TestDbContextFactory.AddDevice(db);

            var user = new User
            {
                Id = Guid.NewGuid(), OrganizationId = TestDbContextFactory.OrganizationId, FullName = "Employee",
                Email = $"{Guid.NewGuid():N}@example.local", PasswordHash = "x", RoleId = 5, UserTypeId = 2, StatusId = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            var employee = new Employee
            {
                Id = Guid.NewGuid(), OrganizationId = TestDbContextFactory.OrganizationId, UserId = user.Id,
                EmployeeNumber = Guid.NewGuid().ToString("N")[..10], DisplayName = "Employee", Email = user.Email,
                StatusId = 1, CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Employees.Add(employee);
            db.DeviceUserAssignments.Add(new DeviceUserAssignment
            {
                Id = Guid.NewGuid(), OrganizationId = TestDbContextFactory.OrganizationId, DeviceId = device.Id,
                EmployeeId = employee.Id, UserSid = string.Empty, IsPrimary = true, AssignedAtUtc = DateTimeOffset.UtcNow,
                AssignedByUserId = admin.Id
            });
            db.SaveChanges();

            AddGrant(db, admin.Id, "usb.storage", "Employee", employee.Id.ToString(), targetDeviceId: device.Id);

            var service = CreateService(db);
            var result = await service.GetPolicyAsync(TestDbContextFactory.OrganizationId, device.Id, currentVersion: 0, userSid: null);

            Assert.Contains(result.Data!.Snapshot!.Policy.Permissions.Grants, g => g.ActionKey == "usb.storage" && g.Allowed);
        }
    }
}
