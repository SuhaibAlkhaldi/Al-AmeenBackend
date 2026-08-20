using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

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

            db.SaveChanges();

            return db;
        }
    }
}
