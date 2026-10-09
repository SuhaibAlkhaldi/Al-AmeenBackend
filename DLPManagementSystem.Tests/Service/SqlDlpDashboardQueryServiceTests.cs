using DLPManagementSystem.CompanyDlpDashboard;
using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Tests.Service
{
    // SqlDlpDashboardQueryService talks to the database via raw ADO.NET (SqlConnection + hand-written
    // T-SQL), not EF - TestDbContextFactory's InMemory provider can't run it at all. Same reasoning as
    // DeviceUserAssignmentConcurrencyTests/PolicyVersionServiceConcurrencyTests for using the real
    // local dev SQL Server database instead.
    //
    // dbo.AuditEvents/GetTotalsAsync/GetSeriesAsync are NOT scoped by OrganizationId (a pre-existing
    // gap, not something this batch introduces or fixes - see the ui-data-batch3 report) - every event
    // in the table within a query's [from, to) window counts, regardless of which organization's test
    // fixture inserted it. To stay isolated from any other data in this shared dev database (past,
    // future, or another test's rows), every test here anchors its window to a far-future, test-unique
    // date far outside any range a real event could ever carry.
    public sealed class SqlDlpDashboardQueryServiceTests : IAsyncLifetime
    {
        private const string ConnectionString =
            "Server=localhost,1433;Database=DLPSystem;User Id=ameen_dev_login;Password=AmeenDev_2026!;TrustServerCertificate=True;Connect Timeout=60;";

        private Guid _organizationId;
        private Guid _deviceId;
        private int _eventTypeId;
        private readonly Dictionary<string, int> _decisionIds = new();

        private static DLPSystemContext CreateContext() =>
            new(new DbContextOptionsBuilder<DLPSystemContext>().UseSqlServer(ConnectionString).Options);

        private static SqlDlpDashboardQueryService CreateService()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", ConnectionString) })
                .Build();

            return new SqlDlpDashboardQueryService(configuration, Options.Create(new DlpDashboardOptions()));
        }

        public async Task InitializeAsync()
        {
            await using var db = CreateContext();

            var activeDeviceStatusId = await db.DeviceStatuses.Where(x => x.Name == "Active").Select(x => x.Id).SingleAsync();

            _eventTypeId = await db.AuditEventTypes.Where(x => x.Name == "PermissionEvaluated").Select(x => x.Id).SingleAsync();
            foreach (var name in new[] { "Allow", "Block", "AuditOnly", "Error" })
            {
                _decisionIds[name] = await db.AuditDecisions.Where(x => x.Name == name).Select(x => x.Id).SingleAsync();
            }
            // Confirms the key exists (seeded by DatabaseSeeder) rather than assuming it, same as the
            // lookups above - a plain SELECT, so it's unaffected by the unrelated Employee/PermissionAction
            // schema drift this local dev database currently has relative to ui-data-batch2's migrations.
            await db.PermissionActions.Where(x => x.Key == "usb.storage").Select(x => x.Key).SingleAsync();

            _organizationId = Guid.NewGuid();
            db.Organizations.Add(new Organization
            {
                Id = _organizationId,
                Name = "Dashboard Series Test Org",
                Code = $"DASH-{_organizationId:N}"[..20],
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            _deviceId = Guid.NewGuid();
            db.Devices.Add(new Device
            {
                Id = _deviceId,
                OrganizationId = _organizationId,
                DeviceKey = Guid.NewGuid().ToString("N"),
                MachineName = "DASH-SERIES-TEST-DEVICE",
                StatusId = activeDeviceStatusId,
                CurrentPolicyVersion = 0,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await using var db = CreateContext();
            await db.AuditEvents.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Devices.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Organizations.Where(x => x.Id == _organizationId).ExecuteDeleteAsync();
        }

        private async Task InsertEventAsync(DateTimeOffset occurredAndReceivedAtUtc, string decisionName)
        {
            await using var db = CreateContext();
            db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                DeviceId = _deviceId,
                ActionKey = "usb.storage",
                EventTypeId = _eventTypeId,
                DecisionId = _decisionIds[decisionName],
                OccurredAtUtc = occurredAndReceivedAtUtc,
                ReceivedAtUtc = occurredAndReceivedAtUtc
            });
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task GetSummaryAsync_RangeIsInclusiveOfFromAndExclusiveOfTo()
        {
            var from = new DateTimeOffset(2035, 6, 1, 10, 0, 0, TimeSpan.Zero);
            var to = from.AddHours(6);

            await InsertEventAsync(from.AddSeconds(-1), "Allow"); // just before -> excluded
            await InsertEventAsync(from, "Allow");                // exactly at from -> included
            await InsertEventAsync(to, "Allow");                  // exactly at to -> excluded

            var summary = await CreateService().GetSummaryAsync(_organizationId, from, to, CancellationToken.None);

            Assert.Equal(1, summary.Totals.TotalEvents);
        }

        [Fact]
        public async Task GetSummaryAsync_PreviousTotals_CoversTheImmediatelyPrecedingPeriodOfEqualLength()
        {
            var from = new DateTimeOffset(2035, 7, 1, 0, 0, 0, TimeSpan.Zero);
            var to = from.AddHours(24);

            // Current period: 2 events.
            await InsertEventAsync(from.AddHours(1), "Allow");
            await InsertEventAsync(from.AddHours(2), "Block");

            // Previous period ([from-24h, from)): 3 events.
            await InsertEventAsync(from.AddHours(-1), "Allow");
            await InsertEventAsync(from.AddHours(-2), "Allow");
            await InsertEventAsync(from.AddHours(-3), "Block");

            // Outside both periods entirely - must not leak into either count.
            await InsertEventAsync(from.AddHours(-25), "Allow");

            var summary = await CreateService().GetSummaryAsync(_organizationId, from, to, CancellationToken.None);

            Assert.Equal(2, summary.Totals.TotalEvents);
            Assert.NotNull(summary.PreviousTotals);
            Assert.Equal(3, summary.PreviousTotals!.TotalEvents);
            Assert.Equal(2, summary.PreviousTotals.AllowedEvents);
            Assert.Equal(1, summary.PreviousTotals.BlockedEvents);
        }

        [Fact]
        public async Task GetSummaryAsync_Series_UsesHourlyBucketsAndZeroFillsEmptyOnesForA6HourRange()
        {
            var from = new DateTimeOffset(2035, 8, 1, 0, 0, 0, TimeSpan.Zero);
            var to = from.AddHours(6);

            await InsertEventAsync(from.AddHours(1).AddMinutes(30), "Block");
            await InsertEventAsync(from.AddHours(1).AddMinutes(45), "Block");
            await InsertEventAsync(from.AddHours(4).AddMinutes(10), "Allow");

            var summary = await CreateService().GetSummaryAsync(_organizationId, from, to, CancellationToken.None);

            Assert.NotNull(summary.Series);
            var series = summary.Series!;
            Assert.Equal(6, series.Count); // one bucket per hour across [00:00, 06:00)

            for (var i = 0; i < series.Count; i++)
            {
                Assert.Equal(from.AddHours(i), series[i].BucketStartUtc);
            }

            Assert.Equal(0, series[0].Total);
            Assert.Equal(2, series[1].Total);
            Assert.Equal(2, series[1].Blocked);
            Assert.Equal(0, series[2].Total);
            Assert.Equal(0, series[3].Total);
            Assert.Equal(1, series[4].Total);
            Assert.Equal(1, series[4].Allowed);
            Assert.Equal(0, series[5].Total);
        }

        [Fact]
        public async Task GetSummaryAsync_Series_UsesDailyBucketsForA168HourRange()
        {
            var from = new DateTimeOffset(2035, 9, 1, 0, 0, 0, TimeSpan.Zero);
            var to = from.AddHours(168); // 7 days

            await InsertEventAsync(from.AddDays(2).AddHours(5), "AuditOnly");

            var summary = await CreateService().GetSummaryAsync(_organizationId, from, to, CancellationToken.None);

            Assert.NotNull(summary.Series);
            var series = summary.Series!;
            Assert.Equal(7, series.Count); // one bucket per day across 7 days

            for (var i = 0; i < series.Count; i++)
            {
                Assert.Equal(from.AddDays(i), series[i].BucketStartUtc);
            }

            Assert.Equal(1, series[2].Total);
            Assert.Equal(1, series[2].AuditOnly);
            Assert.Equal(0, series[0].Total);
            Assert.Equal(0, series[6].Total);
        }

        [Fact]
        public async Task GetSummaryAsync_EmptyRange_ReturnsZeroTotalsAndAFullyZeroFilledSeries()
        {
            var from = new DateTimeOffset(2035, 10, 1, 0, 0, 0, TimeSpan.Zero);
            var to = from.AddHours(24);
            // No events inserted anywhere in or around this window.

            var summary = await CreateService().GetSummaryAsync(_organizationId, from, to, CancellationToken.None);

            Assert.Equal(0, summary.Totals.TotalEvents);
            Assert.NotNull(summary.PreviousTotals);
            Assert.Equal(0, summary.PreviousTotals!.TotalEvents);

            Assert.NotNull(summary.Series);
            var series = summary.Series!;
            Assert.Equal(24, series.Count); // still a complete series, not an empty list
            Assert.All(series, p => Assert.Equal(0, p.Total));
        }
    }
}
