using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;

namespace DLPManagementSystem.Tests.TestHelpers
{
    // Builds a fresh, isolated in-memory DLPSystemContext (unique database name per call) pre-seeded
    // with the fixed lookup rows (Roles/UserTypes/UserStatuses/EmployeeStatuses) and a single
    // Organization that EmployeeService/UserService/AuthService all query by name/id - mirrors what
    // DatabaseSeeder.cs sets up against a real database, just enough of it for these tests.
    public static class TestDbContextFactory
    {
        public static readonly Guid OrganizationId = Guid.NewGuid();

        public static DLPSystemContext Create()
        {
            var options = new DbContextOptionsBuilder<DLPSystemContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                // EmployeeService/UserService wrap their create paths in a real
                // Database.BeginTransactionAsync() (added after a live audit found concurrent device
                // assignments could otherwise leave an orphaned, device-less account committed despite
                // the API reporting failure - see EmployeeService.CreateEmployeeAsync). The InMemory
                // provider has no real transaction support and throws on
                // BeginTransactionAsync/CommitAsync/RollbackAsync by default; against the real SQL
                // Server provider these are genuine, meaningful transactions, so this only silences the
                // InMemory-specific warning rather than changing production behavior.
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var db = new DLPSystemContext(options);

            db.Organizations.Add(new Organization
            {
                Id = OrganizationId,
                Name = "Test Org",
                Code = "TEST-ORG",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            var roleId = 1;
            foreach (var name in new[] { "SuperAdmin", "SecurityAdmin", "HelpDesk", "Auditor", "Employee" })
            {
                db.Roles.Add(new Role { Id = roleId++, Name = name, DisplayName = name, IsActive = true });
            }

            db.UserTypes.Add(new UserType { Id = 1, Name = "Admin", DisplayName = "Admin" });
            db.UserTypes.Add(new UserType { Id = 2, Name = "Employee", DisplayName = "Employee" });

            db.UserStatuses.Add(new UserStatus { Id = 1, Name = "Active", DisplayName = "Active" });
            db.UserStatuses.Add(new UserStatus { Id = 2, Name = "Disabled", DisplayName = "Disabled" });

            db.EmployeeStatuses.Add(new EmployeeStatus { Id = 1, Name = "Active", DisplayName = "Active" });
            db.EmployeeStatuses.Add(new EmployeeStatus { Id = 2, Name = "Suspended", DisplayName = "Suspended" });
            db.EmployeeStatuses.Add(new EmployeeStatus { Id = 3, Name = "Terminated", DisplayName = "Terminated" });

            db.DeviceStatuses.Add(new DeviceStatus { Id = 1, Name = "PendingEnrollment", DisplayName = "Pending Enrollment" });
            db.DeviceStatuses.Add(new DeviceStatus { Id = 2, Name = "Active", DisplayName = "Active" });
            db.DeviceStatuses.Add(new DeviceStatus { Id = 3, Name = "Disabled", DisplayName = "Disabled" });
            db.DeviceStatuses.Add(new DeviceStatus { Id = 4, Name = "Lost", DisplayName = "Lost" });
            db.DeviceStatuses.Add(new DeviceStatus { Id = 5, Name = "Retired", DisplayName = "Retired" });

            db.PermissionDecisions.Add(new PermissionDecision { Id = 1, Name = "Allow", DisplayName = "Allow" });
            db.PermissionDecisions.Add(new PermissionDecision { Id = 2, Name = "Deny", DisplayName = "Deny" });

            db.PermissionGrantTypes.Add(new PermissionGrantType { Id = 1, Name = "Permanent", DisplayName = "Permanent" });
            db.PermissionGrantTypes.Add(new PermissionGrantType { Id = 2, Name = "Temporary", DisplayName = "Temporary" });

            db.PermissionSubjectTypes.Add(new PermissionSubjectType { Id = 1, Name = "Organization", DisplayName = "Organization" });
            db.PermissionSubjectTypes.Add(new PermissionSubjectType { Id = 3, Name = "Employee", DisplayName = "Employee" });
            db.PermissionSubjectTypes.Add(new PermissionSubjectType { Id = 5, Name = "Device", DisplayName = "Device" });

            db.PermissionActionCategories.Add(new PermissionActionCategory { Id = 4, Name = "Usb", DisplayName = "USB" });
            db.PermissionActionCategories.Add(new PermissionActionCategory { Id = 3, Name = "Screen", DisplayName = "Screen" });

            db.PermissionActions.Add(new PermissionAction
            {
                Key = "usb.storage",
                CategoryId = 4,
                DisplayName = "USB Storage",
                DefaultDecisionId = 2,
                SupportsPermanentGrant = true,
                SupportsTemporaryGrant = true,
                IsEnabled = true,
                SortOrder = 100
            });
            db.PermissionActions.Add(new PermissionAction
            {
                Key = "screen.capture",
                CategoryId = 3,
                DisplayName = "Screen Capture",
                DefaultDecisionId = 2,
                SupportsPermanentGrant = true,
                SupportsTemporaryGrant = true,
                IsEnabled = true,
                SortOrder = 70
            });
            db.PermissionActions.Add(new PermissionAction
            {
                Key = "disabled.action",
                CategoryId = 3,
                DisplayName = "Disabled Action",
                DefaultDecisionId = 2,
                SupportsPermanentGrant = true,
                SupportsTemporaryGrant = true,
                IsEnabled = false,
                SortOrder = 999
            });

            db.SaveChanges();

            return db;
        }

        // DeviceStatusId defaults to 2 ("Active") - pass a different one (e.g. 1, "PendingEnrollment")
        // to exercise the "device exists but isn't Active" rejection path.
        public static Device AddDevice(DLPSystemContext db, int deviceStatusId = 2, Guid? organizationId = null)
        {
            var device = new Device
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId ?? OrganizationId,
                DeviceKey = Guid.NewGuid().ToString("N"),
                MachineName = "TEST-MACHINE",
                StatusId = deviceStatusId,
                CurrentPolicyVersion = 0,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Devices.Add(device);
            db.SaveChanges();
            return device;
        }
    }
}
