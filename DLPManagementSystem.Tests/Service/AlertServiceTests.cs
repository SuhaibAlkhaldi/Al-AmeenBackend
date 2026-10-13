using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the ui-data-batch1 addition: AlertListItemDto/AlertDetailDto.Category, derived from
    // Alert -> AuditEvent -> ActionKeyNavigation (PermissionAction) -> Category - the same lookup
    // chain the dashboard's ChannelBreakdown already uses, now surfaced on Alerts too.
    public class AlertServiceTests
    {
        private static AlertService CreateService(DLPSystemContext db) => new(db);

        // TestDbContextFactory already seeds PermissionActionCategories (Usb=4, Screen=3) and two
        // PermissionActions ("usb.storage" -> Usb, "screen.capture" -> Screen) - reused here rather
        // than adding a third lookup set just for this test.
        private static Alert AddAlertWithActionKey(DLPSystemContext db, string actionKey)
        {
            var level = new AlertLevel { Id = 1, Name = "Medium", MinRiskScore = 40, MaxRiskScore = 69 };
            var status = new AlertStatus { Id = 1, Name = "New" };
            var eventType = new AuditEventType { Id = 1, Name = "PermissionCheck", DisplayName = "Permission Check" };
            var decision = new AuditDecision { Id = 1, Name = "Block", DisplayName = "Block" };
            var device = TestDbContextFactory.AddDevice(db);

            db.AlertLevels.Add(level);
            db.AlertStatuses.Add(status);
            db.AuditEventTypes.Add(eventType);
            db.AuditDecisions.Add(decision);
            db.SaveChanges();

            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                DeviceId = device.Id,
                ActionKey = actionKey,
                EventTypeId = eventType.Id,
                DecisionId = decision.Id,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                ReceivedAtUtc = DateTimeOffset.UtcNow
            };
            db.AuditEvents.Add(auditEvent);

            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                AuditEventId = auditEvent.Id,
                AlertLevelId = level.Id,
                AlertStatusId = status.Id,
                Title = "Test alert",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Alerts.Add(alert);
            db.SaveChanges();

            return alert;
        }

        [Fact]
        public async Task GetAlertsAsync_PopulatesCategoryFromTheTriggeringActionKey()
        {
            var db = TestDbContextFactory.Create();
            AddAlertWithActionKey(db, "usb.storage");
            var service = CreateService(db);

            var result = await service.GetAlertsAsync(
                TestDbContextFactory.OrganizationId, statusId: null, levelId: null, assignedToUserId: null,
                fromUtc: null, toUtc: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal("USB", item.Category);
        }

        [Fact]
        public async Task GetAlertsAsync_DifferentActionKey_ReflectsItsOwnCategory()
        {
            var db = TestDbContextFactory.Create();
            AddAlertWithActionKey(db, "screen.capture");
            var service = CreateService(db);

            var result = await service.GetAlertsAsync(
                TestDbContextFactory.OrganizationId, statusId: null, levelId: null, assignedToUserId: null,
                fromUtc: null, toUtc: null, page: 1, pageSize: 20);

            Assert.True(result.Success);
            var item = Assert.Single(result.Data!.Items);
            Assert.Equal("Screen", item.Category);
        }

        [Fact]
        public async Task GetAlertByIdAsync_AlsoPopulatesCategory()
        {
            var db = TestDbContextFactory.Create();
            var alert = AddAlertWithActionKey(db, "usb.storage");
            var service = CreateService(db);

            var result = await service.GetAlertByIdAsync(TestDbContextFactory.OrganizationId, alert.Id);

            Assert.True(result.Success);
            Assert.Equal("USB", result.Data!.Category);
        }
    }
}
