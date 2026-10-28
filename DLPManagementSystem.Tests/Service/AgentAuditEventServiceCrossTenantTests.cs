using System.Text.Json;
using DLPManagementSystem.DTO.AgentAuditEvents;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DLPManagementSystem.Tests.Service
{
    // Regression coverage for the tenant-isolation fix in AgentAuditEventService.ReceiveAuditEventBatchAsync:
    // the employeeBySid lookup over EmployeeWindowsIdentities must filter by the caller's own
    // OrganizationId BEFORE grouping/First(). The unique index on that table is
    // (OrganizationId, UserSid) - NOT UserSid alone - so the exact same Windows SID can legitimately be
    // active in two different organizations at once. Without the org filter, GroupBy(UserSid).First()
    // could resolve to the WRONG organization's employee, and that EmployeeId would then be written onto
    // THIS organization's own AuditEvent row - a cross-tenant PII leak the moment anything joins
    // AuditEvent.Employee to display a name (AuditEventService, AlertService, exports).
    public class AgentAuditEventServiceCrossTenantTests
    {
        private const string SharedSid = "S-1-5-21-123456789-123456789-123456789-1001";

        private static (Guid OrgId, Employee Employee, Device Device) SeedOrgWithSidMapping(
            DLPSystemContext db, string employeeDisplayName)
        {
            var orgId = Guid.NewGuid();
            db.Organizations.Add(new Organization
            {
                Id = orgId,
                Name = employeeDisplayName + "'s Org",
                Code = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                FullName = employeeDisplayName,
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
                OrganizationId = orgId,
                UserId = user.Id,
                EmployeeNumber = Guid.NewGuid().ToString("N")[..10],
                DisplayName = employeeDisplayName,
                Email = user.Email,
                StatusId = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Employees.Add(employee);

            db.EmployeeWindowsIdentities.Add(new EmployeeWindowsIdentity
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                EmployeeId = employee.Id,
                Username = employeeDisplayName,
                UserSid = SharedSid,
                IsPrimary = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RevokedAtUtc = null
            });

            var device = TestDbContextFactory.AddDevice(db, organizationId: orgId);

            db.SaveChanges();

            return (orgId, employee, device);
        }

        private static void SeedSharedAuditLookups(DLPSystemContext db)
        {
            // Global catalog rows (not org-scoped) - one copy shared by every organization, exactly
            // like DatabaseSeeder's idempotent "add if missing" lookups in production.
            db.AuditDecisions.Add(new AuditDecision { Id = 1, Name = "Allow", DisplayName = "Allow" });
            db.AuditEventTypes.Add(new AuditEventType { Id = 1, Name = "ActionAllowed", DisplayName = "Action Allowed" });
            db.SaveChanges();
        }

        private static AgentAuditEventService CreateService(DLPSystemContext db) =>
            new(db, NullLogger<AgentAuditEventService>.Instance);

        private static AgentAuditBatchRequestDto BuildBatch(Guid orgId, Guid deviceId, Guid eventId) => new()
        {
            TenantId = orgId,
            DeviceId = deviceId,
            AgentVersion = "1.0.0",
            Events = new List<SecurityEventEnvelopeDto>
            {
                new()
                {
                    EventId = eventId,
                    CorrelationId = Guid.NewGuid(),
                    ProtocolVersion = "1",
                    EventSchemaVersion = "1",
                    TenantId = orgId,
                    DeviceId = deviceId,
                    UserSid = SharedSid,
                    ActionKey = "usb.storage",
                    EventType = "ActionAllowed",
                    Decision = "Allow",
                    ReasonCode = string.Empty,
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    AgentVersion = "1.0.0",
                    IntegrityHash = string.Empty
                }
            }
        };

        // No "events" property, so ReceiveAuditEventBatchAsync's rawEvents list stays empty and every
        // event's IntegrityVerified is simply left null (not evaluated) - irrelevant to what this test
        // is checking.
        private static readonly JsonElement EmptyRawBody = JsonDocument.Parse("{}").RootElement;

        [Fact]
        public async Task ReceiveAuditEventBatchAsync_SidCollisionAcrossOrgs_OrgADeviceResolvesOrgAEmployee()
        {
            var db = TestDbContextFactory.Create();
            SeedSharedAuditLookups(db);
            var (orgAId, employeeA, deviceA) = SeedOrgWithSidMapping(db, "Org A Employee");
            var (_, employeeB, _) = SeedOrgWithSidMapping(db, "Org B Employee");
            var service = CreateService(db);
            var eventId = Guid.NewGuid();

            var result = await service.ReceiveAuditEventBatchAsync(
                orgAId, deviceA.Id, BuildBatch(orgAId, deviceA.Id, eventId), EmptyRawBody);

            Assert.True(result.Success);
            Assert.Contains(eventId, result.Data!.AcceptedEventIds);

            var savedEvent = await db.AuditEvents.SingleAsync(x => x.Id == eventId);
            Assert.Equal(employeeA.Id, savedEvent.EmployeeId);
            Assert.NotEqual(employeeB.Id, savedEvent.EmployeeId);
        }

        [Fact]
        public async Task ReceiveAuditEventBatchAsync_SidCollisionAcrossOrgs_OrgBDeviceResolvesOrgBEmployee()
        {
            // Reverse of the test above - seeded in the same order (org A first, org B second), so a
            // naive GroupBy(UserSid).First() with no stable ordering guarantee would be most likely to
            // leak org A's employee onto org B's device if the fix regressed; this direction catches that.
            var db = TestDbContextFactory.Create();
            SeedSharedAuditLookups(db);
            var (_, employeeA, _) = SeedOrgWithSidMapping(db, "Org A Employee");
            var (orgBId, employeeB, deviceB) = SeedOrgWithSidMapping(db, "Org B Employee");
            var service = CreateService(db);
            var eventId = Guid.NewGuid();

            var result = await service.ReceiveAuditEventBatchAsync(
                orgBId, deviceB.Id, BuildBatch(orgBId, deviceB.Id, eventId), EmptyRawBody);

            Assert.True(result.Success);
            Assert.Contains(eventId, result.Data!.AcceptedEventIds);

            var savedEvent = await db.AuditEvents.SingleAsync(x => x.Id == eventId);
            Assert.Equal(employeeB.Id, savedEvent.EmployeeId);
            Assert.NotEqual(employeeA.Id, savedEvent.EmployeeId);
        }
    }
}
