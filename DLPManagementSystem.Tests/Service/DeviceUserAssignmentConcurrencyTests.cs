using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Devices;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the fix for DeviceService.AssignDeviceAsync's TOCTOU race: it used to read the device's
    // currently-active assignments, mark them unassigned in memory, and only then insert the new one -
    // two concurrent calls for the same device (different employees) could both read "no active owner
    // yet" before either committed, and both succeed, leaving two simultaneously-active assignments for
    // one device. Found live during a race-condition audit of the (unrelated) PolicyVersions fix.
    // UQ_DeviceUserAssignments_Device_ActivePrimary now makes that state impossible at the database
    // level regardless of what the application code does - these tests exercise both the real service
    // (the race as it actually happens) and the raw index itself (the guarantee independent of any
    // particular caller's logic).
    //
    // Same reasoning as PolicyVersionServiceConcurrencyTests for using the real local dev SQL Server
    // database instead of TestDbContextFactory's InMemory provider: a unique filtered index is a real
    // database-level constraint InMemory doesn't enforce at all.
    public sealed class DeviceUserAssignmentConcurrencyTests : IAsyncLifetime
    {
        private const string ConnectionString =
            "Server=localhost,1433;Database=DLPSystem;User Id=ameen_dev_login;Password=AmeenDev_2026!;TrustServerCertificate=True;Connect Timeout=60;";

        private Guid _organizationId;
        private Guid _deviceId;
        private Guid _employeeAId;
        private Guid _employeeBId;
        private Guid _assignerUserId;

        private static DLPSystemContext CreateContext() =>
            new(new DbContextOptionsBuilder<DLPSystemContext>().UseSqlServer(ConnectionString).Options);

        public async Task InitializeAsync()
        {
            await using var db = CreateContext();

            var employeeRoleId = await db.Roles.Where(x => x.Name == "Employee").Select(x => x.Id).SingleAsync();
            var superAdminRoleId = await db.Roles.Where(x => x.Name == "SuperAdmin").Select(x => x.Id).SingleAsync();
            var employeeUserTypeId = await db.UserTypes.Where(x => x.Name == "Employee").Select(x => x.Id).SingleAsync();
            var adminUserTypeId = await db.UserTypes.Where(x => x.Name == "Admin").Select(x => x.Id).SingleAsync();
            var activeUserStatusId = await db.UserStatuses.Where(x => x.Name == "Active").Select(x => x.Id).SingleAsync();
            var activeEmployeeStatusId = await db.EmployeeStatuses.Where(x => x.Name == "Active").Select(x => x.Id).SingleAsync();
            var activeDeviceStatusId = await db.DeviceStatuses.Where(x => x.Name == "Active").Select(x => x.Id).SingleAsync();

            _organizationId = Guid.NewGuid();
            db.Organizations.Add(new Organization
            {
                Id = _organizationId,
                Name = "Device Assignment Concurrency Test Org",
                Code = $"DACT-{_organizationId:N}"[..20],
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            _deviceId = Guid.NewGuid();
            db.Devices.Add(new Device
            {
                Id = _deviceId,
                OrganizationId = _organizationId,
                DeviceKey = Guid.NewGuid().ToString("N"),
                MachineName = "CONC-TEST-DEVICE",
                StatusId = activeDeviceStatusId,
                CurrentPolicyVersion = 0,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            (_employeeAId, _employeeBId) = (Guid.NewGuid(), Guid.NewGuid());
            foreach (var (employeeId, label) in new[] { (_employeeAId, "A"), (_employeeBId, "B") })
            {
                var userId = Guid.NewGuid();
                db.Users.Add(new User
                {
                    Id = userId,
                    OrganizationId = _organizationId,
                    FullName = $"Concurrency Test Employee {label}",
                    Email = $"{Guid.NewGuid():N}@example.local",
                    PasswordHash = "irrelevant-for-this-test",
                    UserTypeId = employeeUserTypeId,
                    RoleId = employeeRoleId,
                    StatusId = activeUserStatusId,
                    IsEmailVerified = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
                db.Employees.Add(new Employee
                {
                    Id = employeeId,
                    OrganizationId = _organizationId,
                    UserId = userId,
                    EmployeeNumber = $"CONC-{label}-{employeeId:N}"[..20],
                    DisplayName = $"Concurrency Test Employee {label}",
                    Email = $"{Guid.NewGuid():N}@example.local",
                    StatusId = activeEmployeeStatusId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }

            // The caller performing the assignment (AssignDeviceAsync's assignedByUserId) - a User row,
            // distinct from the Employee rows above (which each have their own User row via UserId).
            _assignerUserId = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = _assignerUserId,
                OrganizationId = _organizationId,
                FullName = "Concurrency Test Assigner",
                Email = $"{Guid.NewGuid():N}@example.local",
                PasswordHash = "irrelevant-for-this-test",
                UserTypeId = adminUserTypeId,
                RoleId = superAdminRoleId,
                StatusId = activeUserStatusId,
                IsEmailVerified = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await using var db = CreateContext();
            // FK order: DeviceUserAssignments -> (PolicyChangeLogs -> PolicyVersions), Employees, Users -> Device, Organization.
            await db.DeviceUserAssignments.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.PolicyChangeLogs.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.PolicyVersions.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Employees.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Users.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Devices.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Organizations.Where(x => x.Id == _organizationId).ExecuteDeleteAsync();
        }

        [Fact]
        public async Task AssignDeviceAsync_ConcurrentCallsForDifferentEmployeesSameDevice_OnlyOneSucceeds()
        {
            await using var dbA = CreateContext();
            var serviceA = new DeviceService(dbA, new PolicyVersionService(dbA), new FakeAdminAuditLogService());
            var taskA = serviceA.AssignDeviceAsync(
                _organizationId, _deviceId, _assignerUserId,
                new AssignDeviceDto { EmployeeId = _employeeAId }, CancellationToken.None);

            await using var dbB = CreateContext();
            var serviceB = new DeviceService(dbB, new PolicyVersionService(dbB), new FakeAdminAuditLogService());
            var taskB = serviceB.AssignDeviceAsync(
                _organizationId, _deviceId, _assignerUserId,
                new AssignDeviceDto { EmployeeId = _employeeBId }, CancellationToken.None);

            var results = await Task.WhenAll(taskA, taskB);

            // Neither call should have thrown - a caller losing the race must get a clear, handled
            // failure response, not an unhandled exception surfacing as a generic 500.
            Assert.Single(results, r => r.Success);
            var loser = Assert.Single(results, r => !r.Success);
            Assert.Equal(
                "This device was just assigned to another employee. Please refresh and try again.",
                loser.MessageEn);
            Assert.Equal(
                "تم تعيين هذا الجهاز لموظف آخر للتو. الرجاء تحديث الصفحة والمحاولة مرة أخرى",
                loser.MessageAr);

            await using var verifyDb = CreateContext();
            var activeCount = await verifyDb.DeviceUserAssignments
                .Where(x => x.DeviceId == _deviceId && x.UnassignedAtUtc == null)
                .CountAsync();

            Assert.Equal(1, activeCount);
        }

        [Fact]
        public async Task UniqueIndex_TwoActivePrimaryAssignmentsSameDevice_SecondInsertIsRejected()
        {
            // Bypasses DeviceService entirely - proves the guarantee lives in the database schema, not
            // just in application code that happens to check for it today.
            await using var db1 = CreateContext();
            db1.DeviceUserAssignments.Add(new DeviceUserAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                DeviceId = _deviceId,
                EmployeeId = _employeeAId,
                UserSid = string.Empty,
                IsPrimary = true,
                AssignedAtUtc = DateTimeOffset.UtcNow
            });
            await db1.SaveChangesAsync();

            await using var db2 = CreateContext();
            db2.DeviceUserAssignments.Add(new DeviceUserAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                DeviceId = _deviceId,
                EmployeeId = _employeeBId,
                UserSid = string.Empty,
                IsPrimary = true,
                AssignedAtUtc = DateTimeOffset.UtcNow
            });

            var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db2.SaveChangesAsync());
            Assert.True(DbExceptionHelper.IsUniqueConstraintViolationOfIndex(ex, "UQ_DeviceUserAssignments_Device_ActivePrimary"));
        }
    }
}
