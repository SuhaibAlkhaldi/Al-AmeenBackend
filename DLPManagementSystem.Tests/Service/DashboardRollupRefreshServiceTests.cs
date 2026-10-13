using System.Data;
using System.Reflection;
using DLPManagementSystem.CompanyDlpDashboard;
using DLPManagementSystem.CompanyDlpDashboard.Rollup;
using DLPManagementSystem.Helper.Retention;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.BackgroundServices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Tests.Service
{
    // dashboard-rollup: exercises DashboardRollupRefreshService, RollupDlpDashboardQueryService and
    // RollupAwareDlpDashboardQueryService against the real local dev SQL Server database - same
    // reasoning as SqlDlpDashboardQueryServiceSplitEndpointsTests (raw ADO.NET on both the raw and
    // rollup read paths; InMemory can't run either). None of the three services here accept an
    // injectable clock (all call DateTimeOffset.UtcNow directly, matching production), so every
    // test anchors to the real "now" and either (a) picks test data solidly clear of any window
    // edge by more than the documented drift (weekly-trend: 1 hour; risk: 1 day), or (b) for
    // kpis/breakdown, builds the raw path's own DashboardRange with the exact same hour-floored
    // from/to the rollup path computes internally, so that comparison has zero slack.
    public sealed class DashboardRollupRefreshServiceTests : IAsyncLifetime
    {
        private const string ConnectionString =
            "Server=localhost,1433;Database=DLPSystem;User Id=ameen_dev_login;Password=AmeenDev_2026!;TrustServerCertificate=True;Connect Timeout=60;";

        private Guid _orgAId;
        private Guid _orgBId;
        private Guid _deviceAId;
        private Guid _deviceBId;
        private Guid _employeeAId;
        private Guid _employeeBId;
        private readonly Dictionary<string, int> _decisionIds = new();

        private static DLPSystemContext CreateContext() =>
            new(new DbContextOptionsBuilder<DLPSystemContext>().UseSqlServer(ConnectionString).Options);

        private static IConfiguration CreateConfiguration() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", ConnectionString) })
                .Build();

        private static DashboardRollupOptions DefaultRollupOptions() => new()
        {
            UseRollup = false,
            RefreshEnabled = false,
            RefreshIntervalSeconds = 60,
            LateEventLookbackHours = 48,
            EmployeeRollupRecomputeDays = 3,
            RiskLookbackDays = 30,
            RefreshCommandTimeoutSeconds = 30,
            BackfillCommandTimeoutSeconds = 120,
            BackfillChunkDays = 1,
            DistributedLockTimeoutMilliseconds = 0,
            // Off by default here so every test in this file that doesn't explicitly exercise the
            // wide sweep stays deterministic regardless of what UTC hour the test suite happens to
            // run in - only the dedicated wide-sweep tests below turn this on.
            EmployeeRollupWideSweepEnabled = false
        };

        private static DashboardRollupRefreshService CreateRefreshService(DashboardRollupOptions? options = null) =>
            new(CreateConfiguration(), Options.Create(options ?? DefaultRollupOptions()), NullLogger<DashboardRollupRefreshService>.Instance);

        private static RollupDlpDashboardQueryService CreateRollupQueryService(DashboardRollupOptions? options = null) =>
            new(CreateConfiguration(), Options.Create(options ?? DefaultRollupOptions()));

        private static SqlDlpDashboardQueryService CreateRawQueryService() =>
            new(CreateConfiguration(), Options.Create(new DlpDashboardOptions()));

        private static DateTimeOffset FloorToHour(DateTimeOffset value)
        {
            var utc = value.UtcDateTime;
            return new DateTimeOffset(new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);
        }

        public async Task InitializeAsync()
        {
            await using var db = CreateContext();

            var activeDeviceStatusId = await db.DeviceStatuses.Where(x => x.Name == "Active").Select(x => x.Id).SingleAsync();
            var employeeRoleId = await db.Roles.Where(x => x.Name == "Employee").Select(x => x.Id).SingleAsync();
            var employeeUserTypeId = await db.UserTypes.Where(x => x.Name == "Employee").Select(x => x.Id).SingleAsync();
            var activeUserStatusId = await db.UserStatuses.Where(x => x.Name == "Active").Select(x => x.Id).SingleAsync();
            var activeEmployeeStatusId = await db.EmployeeStatuses.Where(x => x.Name == "Active").Select(x => x.Id).SingleAsync();

            foreach (var name in new[] { "Allow", "Block", "AuditOnly", "Error" })
            {
                _decisionIds[name] = await db.AuditDecisions.Where(x => x.Name == name).Select(x => x.Id).SingleAsync();
            }

            await db.PermissionActions.Where(x => x.Key == "usb.storage").Select(x => x.Key).SingleAsync();

            _orgAId = Guid.NewGuid();
            _orgBId = Guid.NewGuid();
            foreach (var (orgId, label) in new[] { (_orgAId, "A"), (_orgBId, "B") })
            {
                db.Organizations.Add(new Organization
                {
                    Id = orgId,
                    Name = $"Dashboard Rollup Test Org {label}",
                    Code = $"DRU{label}-{orgId:N}"[..20],
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }

            _deviceAId = Guid.NewGuid();
            _deviceBId = Guid.NewGuid();
            foreach (var (deviceId, orgId, label) in new[] { (_deviceAId, _orgAId, "A"), (_deviceBId, _orgBId, "B") })
            {
                db.Devices.Add(new Device
                {
                    Id = deviceId,
                    OrganizationId = orgId,
                    DeviceKey = Guid.NewGuid().ToString("N"),
                    MachineName = $"DRU-TEST-DEVICE-{label}",
                    StatusId = activeDeviceStatusId,
                    CurrentPolicyVersion = 0,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }

            _employeeAId = Guid.NewGuid();
            _employeeBId = Guid.NewGuid();
            foreach (var (employeeId, orgId, label) in new[] { (_employeeAId, _orgAId, "A"), (_employeeBId, _orgBId, "B") })
            {
                var userId = Guid.NewGuid();
                db.Users.Add(new User
                {
                    Id = userId,
                    OrganizationId = orgId,
                    FullName = $"DRU Test Employee {label}",
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
                    OrganizationId = orgId,
                    UserId = userId,
                    EmployeeNumber = $"DRU-{label}-{employeeId:N}"[..20],
                    DisplayName = $"DRU Test Employee {label}",
                    Email = $"{Guid.NewGuid():N}@example.local",
                    StatusId = activeEmployeeStatusId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await using var db = CreateContext();
            foreach (var orgId in new[] { _orgAId, _orgBId })
            {
                await db.AuditEvents.Where(x => x.OrganizationId == orgId).ExecuteDeleteAsync();
                await db.DashboardHourlyRollups.Where(x => x.OrganizationId == orgId).ExecuteDeleteAsync();
                await db.DashboardEmployeeDailyRollups.Where(x => x.OrganizationId == orgId).ExecuteDeleteAsync();
                await db.DashboardRollupWatermarks.Where(x => x.OrganizationId == orgId).ExecuteDeleteAsync();
                await db.Employees.Where(x => x.OrganizationId == orgId).ExecuteDeleteAsync();
                await db.Users.Where(x => x.OrganizationId == orgId).ExecuteDeleteAsync();
                await db.Devices.Where(x => x.OrganizationId == orgId).ExecuteDeleteAsync();
                await db.Organizations.Where(x => x.Id == orgId).ExecuteDeleteAsync();
            }
        }

        private async Task InsertEventAsync(
            Guid organizationId, Guid deviceId, Guid? employeeId,
            DateTimeOffset occurredAtUtc, DateTimeOffset receivedAtUtc, string decisionName)
        {
            await using var db = CreateContext();
            db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DeviceId = deviceId,
                EmployeeId = employeeId,
                ActionKey = "usb.storage",
                EventTypeId = await db.AuditEventTypes.Where(x => x.Name == "PermissionEvaluated").Select(x => x.Id).SingleAsync(),
                DecisionId = _decisionIds[decisionName],
                OccurredAtUtc = occurredAtUtc,
                ReceivedAtUtc = receivedAtUtc
            });
            await db.SaveChangesAsync();
        }

        private async Task<long> SumHourlyRollupAsync(Guid organizationId, DashboardRollupTimeBasis basis, DateTimeOffset bucketStartUtc)
        {
            await using var db = CreateContext();
            return await db.DashboardHourlyRollups
                .Where(x => x.OrganizationId == organizationId && x.TimeBasis == basis && x.BucketStartUtc == bucketStartUtc)
                .SumAsync(x => (long?)x.EventCount) ?? 0;
        }

        // --- kpis / breakdown: zero-slack equivalence (same hour-aligned window on both sides) -----

        [Fact]
        public async Task RefreshRecentWindowAsync_KpisAndBreakdown_MatchRawQueryForTheSameAlignedWindow()
        {
            var hourFloor = FloorToHour(DateTimeOffset.UtcNow);

            // Current window [hourFloor-24h, hourFloor): 3 events, mixed decisions.
            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-1), hourFloor.AddHours(-1), "Allow");
            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-5), hourFloor.AddHours(-5), "Block");
            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-20), hourFloor.AddHours(-20), "AuditOnly");

            // Previous window [hourFloor-48h, hourFloor-24h): 1 event, for PreviousTotals.
            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-30), hourFloor.AddHours(-30), "Block");

            var refreshResult = await CreateRefreshService().RefreshRecentWindowAsync(CancellationToken.None);
            Assert.True(refreshResult.LockAcquired);

            var range = new DashboardRange(DashboardRangeKind.TwentyFourHours, "24h", hourFloor.AddHours(-24), hourFloor);

            var rawKpis = await CreateRawQueryService().GetKpisAsync(_orgAId, range, CancellationToken.None);
            var rollupKpis = await CreateRollupQueryService().GetKpisAsync(_orgAId, range, CancellationToken.None);

            Assert.Equal(rawKpis.Totals, rollupKpis.Totals);
            Assert.Equal(rawKpis.PreviousTotals, rollupKpis.PreviousTotals);
            Assert.Equal(rawKpis.Series, rollupKpis.Series);

            // The actual numbers, not just "both sides happen to agree".
            Assert.Equal(3, rollupKpis.Totals.TotalEvents);
            Assert.Equal(1, rollupKpis.Totals.AllowedEvents);
            Assert.Equal(1, rollupKpis.Totals.BlockedEvents);
            Assert.Equal(1, rollupKpis.Totals.AuditOnlyEvents);
            Assert.Equal(1, rollupKpis.PreviousTotals.TotalEvents);

            var rawBreakdown = await CreateRawQueryService().GetBreakdownAsync(_orgAId, range, CancellationToken.None);
            var rollupBreakdown = await CreateRollupQueryService().GetBreakdownAsync(_orgAId, range, CancellationToken.None);

            Assert.Equal(rawBreakdown.ActionBreakdown, rollupBreakdown.ActionBreakdown);
            Assert.Equal(rawBreakdown.ReasonBreakdown, rollupBreakdown.ReasonBreakdown);
            Assert.Equal(rawBreakdown.ChannelBreakdown, rollupBreakdown.ChannelBreakdown);
        }

        // --- weekly-trend / risk: historical data populated via backfill (the regular 48h/3-day ----
        // rolling refresh never reaches back this far), compared against the raw path -------------

        [Fact]
        public async Task BackfillThenRead_WeeklyTrend_MatchesRawQueryForTheSameData()
        {
            var nowUtc = DateTimeOffset.UtcNow;

            // Safely inside each of the 4 weekly buckets, clear of edges by more than an hour, so
            // the raw path's continuous window and the rollup's hour-floored window agree exactly.
            await InsertEventAsync(_orgAId, _deviceAId, null, nowUtc.AddDays(-27).AddHours(2), nowUtc.AddDays(-27).AddHours(2), "Block");
            await InsertEventAsync(_orgAId, _deviceAId, null, nowUtc.AddDays(-20).AddHours(2), nowUtc.AddDays(-20).AddHours(2), "AuditOnly");
            await InsertEventAsync(_orgAId, _deviceAId, null, nowUtc.AddDays(-13).AddHours(2), nowUtc.AddDays(-13).AddHours(2), "Allow");
            await InsertEventAsync(_orgAId, _deviceAId, null, nowUtc.AddDays(-6).AddHours(2), nowUtc.AddDays(-6).AddHours(2), "Block");

            // All 4 events are older than the regular refresh's 48h lookback - a single wide backfill
            // chunk (one call, resumed-from-scratch since there's no watermark yet) covers the whole
            // 28-day span this test needs, exactly like the manual admin endpoint would do on day one.
            var options = DefaultRollupOptions();
            options.BackfillChunkDays = 35;
            var refreshService = CreateRefreshService(options);

            var backfill = await refreshService.BackfillOneChunkAsync(_orgAId, DashboardRollupKindSelector.HourlyOccurred, CancellationToken.None);
            Assert.False(backfill.MoreRemaining);
            Assert.Equal(4, backfill.RowsWritten);

            var rawWeeklyTrend = await CreateRawQueryService().GetWeeklyTrendAsync(_orgAId, CancellationToken.None);
            var rollupWeeklyTrend = await CreateRollupQueryService(options).GetWeeklyTrendAsync(_orgAId, CancellationToken.None);

            Assert.Equal(rawWeeklyTrend.Count, rollupWeeklyTrend.Count);
            for (var i = 0; i < rawWeeklyTrend.Count; i++)
            {
                Assert.Equal(rawWeeklyTrend[i].BlockedCount, rollupWeeklyTrend[i].BlockedCount);
                Assert.Equal(rawWeeklyTrend[i].ViolationCount, rollupWeeklyTrend[i].ViolationCount);
            }

            Assert.Equal(1, rollupWeeklyTrend[0].BlockedCount);
            Assert.Equal(1, rollupWeeklyTrend[0].ViolationCount);
            Assert.Equal(0, rollupWeeklyTrend[1].BlockedCount);
            Assert.Equal(1, rollupWeeklyTrend[1].ViolationCount);
            Assert.Equal(0, rollupWeeklyTrend[2].BlockedCount);
            Assert.Equal(0, rollupWeeklyTrend[2].ViolationCount);
            Assert.Equal(1, rollupWeeklyTrend[3].BlockedCount);
            Assert.Equal(1, rollupWeeklyTrend[3].ViolationCount);
        }

        [Fact]
        public async Task BackfillThenRead_Risk_MatchesRawQueryForTheSameData_AndIsolatesOrganizations()
        {
            var nowUtc = DateTimeOffset.UtcNow;

            // Solidly inside the 30-day risk window on both the raw (continuous) and rollup
            // (day-aligned) sides - clear of both edges by more than a day, so the rollup's own
            // ≤1-day drift can never matter for this comparison.
            await InsertEventAsync(_orgAId, _deviceAId, _employeeAId, nowUtc.AddDays(-5), nowUtc.AddDays(-5), "Block");
            await InsertEventAsync(_orgAId, _deviceAId, _employeeAId, nowUtc.AddDays(-10), nowUtc.AddDays(-10), "Block");
            await InsertEventAsync(_orgAId, _deviceAId, _employeeAId, nowUtc.AddDays(-15), nowUtc.AddDays(-15), "Allow");
            // Org B's employee has no events at all - still must not see org A's employee or score.

            var options = DefaultRollupOptions();
            options.BackfillChunkDays = 35;
            var refreshService = CreateRefreshService(options);

            var backfill = await refreshService.BackfillOneChunkAsync(_orgAId, DashboardRollupKindSelector.EmployeeDaily, CancellationToken.None);
            Assert.False(backfill.MoreRemaining);

            var rawRiskA = await CreateRawQueryService().GetRiskAsync(_orgAId, CancellationToken.None);
            var rollupRiskA = await CreateRollupQueryService(options).GetRiskAsync(_orgAId, CancellationToken.None);
            var rawRiskB = await CreateRawQueryService().GetRiskAsync(_orgBId, CancellationToken.None);
            var rollupRiskB = await CreateRollupQueryService(options).GetRiskAsync(_orgBId, CancellationToken.None);

            // DlpRiskUserDto carries a List<DlpRiskFactorDto> (TopFactors); comparing the DTOs
            // directly would use the record's own compiler-generated Equals, which falls back to
            // reference equality for that nested list (two separately-built lists with identical
            // content are never ReferenceEquals) - so each field is compared explicitly instead,
            // with TopFactors compared as a top-level sequence (where xUnit's own
            // IEnumerable-aware Assert.Equal applies).
            Assert.Equal(rawRiskA.TopRiskyUsers.Count, rollupRiskA.TopRiskyUsers.Count);
            for (var i = 0; i < rawRiskA.TopRiskyUsers.Count; i++)
            {
                var expected = rawRiskA.TopRiskyUsers[i];
                var actual = rollupRiskA.TopRiskyUsers[i];
                Assert.Equal(expected.EmployeeId, actual.EmployeeId);
                Assert.Equal(expected.DisplayName, actual.DisplayName);
                Assert.Equal(expected.DepartmentName, actual.DepartmentName);
                Assert.Equal(expected.RiskScore, actual.RiskScore);
                Assert.Equal(expected.RiskLevel, actual.RiskLevel);
                Assert.Equal(expected.TopFactors, actual.TopFactors);
            }
            Assert.Equal(rawRiskA.RiskByDepartment, rollupRiskA.RiskByDepartment);
            Assert.Equal(rawRiskA.HighRiskUserCount, rollupRiskA.HighRiskUserCount);
            Assert.Equal(rawRiskB.TopRiskyUsers.Count, rollupRiskB.TopRiskyUsers.Count);

            Assert.Contains(rollupRiskA.TopRiskyUsers, u => u.EmployeeId == _employeeAId);
            Assert.DoesNotContain(rollupRiskB.TopRiskyUsers, u => u.EmployeeId == _employeeAId);
        }

        // --- idempotency / isolation ---------------------------------------------------------------

        [Fact]
        public async Task RefreshRecentWindowAsync_RunTwiceWithNoNewData_DoesNotDoubleCount()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var hourFloor = FloorToHour(nowUtc);

            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-2), hourFloor.AddHours(-2), "Block");
            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-10), hourFloor.AddHours(-10), "Allow");
            // EmployeeDaily's own window ends at FloorToDay(now) (exclusive) - "today" is never
            // included until the day rolls over, so this needs to be at least a day in the past to
            // land inside [today-3d, today) at all.
            await InsertEventAsync(_orgAId, _deviceAId, _employeeAId, nowUtc.AddDays(-1), nowUtc.AddDays(-1), "Block");

            var refreshService = CreateRefreshService();
            var first = await refreshService.RefreshRecentWindowAsync(CancellationToken.None);
            var second = await refreshService.RefreshRecentWindowAsync(CancellationToken.None);

            Assert.True(first.LockAcquired);
            Assert.True(second.LockAcquired);

            await using var db = CreateContext();
            var hourlySum = await db.DashboardHourlyRollups
                .Where(x => x.OrganizationId == _orgAId && x.TimeBasis == DashboardRollupTimeBasis.Received)
                .SumAsync(x => (long?)x.EventCount) ?? 0;
            var employeeSum = await db.DashboardEmployeeDailyRollups
                .Where(x => x.OrganizationId == _orgAId)
                .SumAsync(x => (long?)x.EventCount) ?? 0;

            Assert.Equal(3, hourlySum);
            Assert.Equal(1, employeeSum);
        }

        [Fact]
        public async Task RefreshRecentWindowAsync_OrganizationIsolation()
        {
            var hourFloor = FloorToHour(DateTimeOffset.UtcNow);

            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-3), hourFloor.AddHours(-3), "Block");
            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-4), hourFloor.AddHours(-4), "Block");
            await InsertEventAsync(_orgBId, _deviceBId, null, hourFloor.AddHours(-3), hourFloor.AddHours(-3), "Allow");

            await CreateRefreshService().RefreshRecentWindowAsync(CancellationToken.None);

            await using var db = CreateContext();
            var sumA = await db.DashboardHourlyRollups
                .Where(x => x.OrganizationId == _orgAId && x.TimeBasis == DashboardRollupTimeBasis.Received)
                .SumAsync(x => (long?)x.EventCount) ?? 0;
            var sumB = await db.DashboardHourlyRollups
                .Where(x => x.OrganizationId == _orgBId && x.TimeBasis == DashboardRollupTimeBasis.Received)
                .SumAsync(x => (long?)x.EventCount) ?? 0;

            Assert.Equal(2, sumA);
            Assert.Equal(1, sumB);

            var range = new DashboardRange(DashboardRangeKind.TwentyFourHours, "24h", hourFloor.AddHours(-24), hourFloor);
            var kpisA = await CreateRollupQueryService().GetKpisAsync(_orgAId, range, CancellationToken.None);
            var kpisB = await CreateRollupQueryService().GetKpisAsync(_orgBId, range, CancellationToken.None);

            Assert.Equal(2, kpisA.Totals.TotalEvents);
            Assert.Equal(2, kpisA.Totals.BlockedEvents);
            Assert.Equal(1, kpisB.Totals.TotalEvents);
            Assert.Equal(0, kpisB.Totals.BlockedEvents);
        }

        // --- late events: within the lookback are picked up automatically; beyond it are not --------

        [Fact]
        public async Task RefreshRecentWindowAsync_LateEventWithinLookback_AppearsAfterTheNextCycle()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var hourFloor = FloorToHour(nowUtc);
            var bucketStart = hourFloor.AddHours(-30); // well within the default 48h lookback

            var refreshService = CreateRefreshService();
            await refreshService.RefreshRecentWindowAsync(CancellationToken.None);

            Assert.Equal(0, await SumHourlyRollupAsync(_orgAId, DashboardRollupTimeBasis.Occurred, bucketStart));

            // Simulates an agent reconnecting and sending an event whose OccurredAtUtc is 30 hours in
            // the past (it was offline) - ReceivedAtUtc is "now", matching AgentAuditEventService.
            await InsertEventAsync(_orgAId, _deviceAId, null, bucketStart.AddMinutes(10), nowUtc, "Block");

            await refreshService.RefreshRecentWindowAsync(CancellationToken.None);

            Assert.Equal(1, await SumHourlyRollupAsync(_orgAId, DashboardRollupTimeBasis.Occurred, bucketStart));
        }

        [Fact]
        public async Task EventOlderThanLateEventLookbackHours_IsNotPickedUpAutomatically_ButBackfillRecoversIt()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var hourFloor = FloorToHour(nowUtc);
            var bucketStart = hourFloor.AddHours(-58); // 10h beyond the default 48h lookback

            await InsertEventAsync(_orgAId, _deviceAId, null, bucketStart.AddMinutes(15), nowUtc, "Block");

            var refreshService = CreateRefreshService();
            await refreshService.RefreshRecentWindowAsync(CancellationToken.None);

            // Not picked up - outside the regular cycle's 48h lookback window, exactly as documented.
            Assert.Equal(0, await SumHourlyRollupAsync(_orgAId, DashboardRollupTimeBasis.Occurred, bucketStart));

            // Resumable backfill, one BackfillChunkDays-wide chunk (default 1 day) at a time, exactly
            // as the manual admin endpoint calls it.
            var moreRemaining = true;
            var chunksProcessed = 0;
            while (moreRemaining && chunksProcessed < 10)
            {
                var chunk = await refreshService.BackfillOneChunkAsync(_orgAId, DashboardRollupKindSelector.HourlyOccurred, CancellationToken.None);
                moreRemaining = chunk.MoreRemaining;
                chunksProcessed++;
            }

            Assert.False(moreRemaining);
            Assert.Equal(1, await SumHourlyRollupAsync(_orgAId, DashboardRollupTimeBasis.Occurred, bucketStart));

            await using var db = CreateContext();
            var watermark = await db.DashboardRollupWatermarks.SingleAsync(
                x => x.OrganizationId == _orgAId && x.RollupKind == DashboardRollupKind.HourlyOccurred.ToString());
            Assert.True(watermark.LastComputedThroughUtc <= bucketStart);
        }

        // --- EffectiveEmployeeRollupRecomputeDays derivation ----------------------------------------

        [Fact]
        public void EffectiveEmployeeRollupRecomputeDays_WhenNotExplicitlySet_DerivesFromLateEventLookbackHours()
        {
            // ceil(48/24)+1 = 3 - the production default (LateEventLookbackHours=48) must derive
            // to 3, matching the documented "48h lookback + a one-day safety margin" intent.
            Assert.Null(new DashboardRollupOptions().EmployeeRollupRecomputeDays);
            Assert.Equal(3, new DashboardRollupOptions().EffectiveEmployeeRollupRecomputeDays);

            Assert.Equal(4, new DashboardRollupOptions { LateEventLookbackHours = 72 }.EffectiveEmployeeRollupRecomputeDays);
            Assert.Equal(10, new DashboardRollupOptions { LateEventLookbackHours = 48, EmployeeRollupRecomputeDays = 10 }.EffectiveEmployeeRollupRecomputeDays);
        }

        [Fact]
        public async Task RefreshRecentWindowAsync_EmployeeEventFortySevenHoursOld_OnADifferentUtcCalendarDay_IsCaptured()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            // 47h is always on a calendar date before today (47 > 24), and often two days before -
            // exactly the "48h spans up to 3 calendar days" case the 3-day default must cover.
            var occurredAt = nowUtc.AddHours(-47);

            await InsertEventAsync(_orgAId, _deviceAId, _employeeAId, occurredAt, nowUtc, "Block");

            // Default options here (EmployeeRollupRecomputeDays left null) - exercising the real
            // production-derived window (3 days), not a hand-picked test value.
            var options = DefaultRollupOptions();
            options.EmployeeRollupRecomputeDays = null;
            await CreateRefreshService(options).RefreshRecentWindowAsync(CancellationToken.None);

            await using var db = CreateContext();
            var sum = await db.DashboardEmployeeDailyRollups
                .Where(x => x.OrganizationId == _orgAId && x.EmployeeId == _employeeAId)
                .SumAsync(x => (long?)x.EventCount) ?? 0;
            Assert.Equal(1, sum);
        }

        // --- daily wide sweep (EmployeeDaily safety net) ---------------------------------------------

        [Fact]
        public async Task MaybeRunEmployeeWideSweepAsync_OnlyRunsInItsConfiguredHour_AndThenCatchesEventsBeyondTheRegularWindow()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            // Well beyond the regular 3-day EmployeeDaily window, but inside the 30-day wide sweep.
            var farOccurredAt = nowUtc.AddDays(-10);
            await InsertEventAsync(_orgAId, _deviceAId, _employeeAId, farOccurredAt, farOccurredAt, "Block");

            var currentHour = nowUtc.UtcDateTime.Hour;
            var wrongHourOptions = DefaultRollupOptions();
            wrongHourOptions.EmployeeRollupWideSweepEnabled = true;
            wrongHourOptions.EmployeeRollupWideSweepHourUtc = (currentHour + 12) % 24;

            await CreateRefreshService(wrongHourOptions).RefreshRecentWindowAsync(CancellationToken.None);

            await using (var db = CreateContext())
            {
                var sum = await db.DashboardEmployeeDailyRollups
                    .Where(x => x.OrganizationId == _orgAId && x.EmployeeId == _employeeAId)
                    .SumAsync(x => (long?)x.EventCount) ?? 0;
                Assert.Equal(0, sum); // wrong hour - the sweep must not have run
            }

            var rightHourOptions = DefaultRollupOptions();
            rightHourOptions.EmployeeRollupWideSweepEnabled = true;
            rightHourOptions.EmployeeRollupWideSweepHourUtc = currentHour;

            await CreateRefreshService(rightHourOptions).RefreshRecentWindowAsync(CancellationToken.None);

            await using (var db = CreateContext())
            {
                var sum = await db.DashboardEmployeeDailyRollups
                    .Where(x => x.OrganizationId == _orgAId && x.EmployeeId == _employeeAId)
                    .SumAsync(x => (long?)x.EventCount) ?? 0;
                Assert.Equal(1, sum); // right hour - the sweep ran and caught the far event

                var watermark = await db.DashboardRollupWatermarks.SingleAsync(
                    x => x.OrganizationId == _orgAId && x.RollupKind == DashboardRollupKind.EmployeeDaily.ToString());
                Assert.NotNull(watermark.LastWideSweepUtc);
            }
        }

        [Fact]
        public async Task MaybeRunEmployeeWideSweepAsync_RunsAtMostOncePerUtcDay_AndNeverWhenDisabled()
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var farOccurredAt = nowUtc.AddDays(-10);
            await InsertEventAsync(_orgAId, _deviceAId, _employeeAId, farOccurredAt, farOccurredAt, "Block");

            var disabledOptions = DefaultRollupOptions();
            disabledOptions.EmployeeRollupWideSweepEnabled = false;
            disabledOptions.EmployeeRollupWideSweepHourUtc = nowUtc.UtcDateTime.Hour;
            await CreateRefreshService(disabledOptions).RefreshRecentWindowAsync(CancellationToken.None);

            await using (var db = CreateContext())
            {
                var sum = await db.DashboardEmployeeDailyRollups
                    .Where(x => x.OrganizationId == _orgAId && x.EmployeeId == _employeeAId)
                    .SumAsync(x => (long?)x.EventCount) ?? 0;
                Assert.Equal(0, sum); // disabled - must never run regardless of the hour
            }

            var enabledOptions = DefaultRollupOptions();
            enabledOptions.EmployeeRollupWideSweepEnabled = true;
            enabledOptions.EmployeeRollupWideSweepHourUtc = nowUtc.UtcDateTime.Hour;
            var refreshService = CreateRefreshService(enabledOptions);

            await refreshService.RefreshRecentWindowAsync(CancellationToken.None);

            DateTimeOffset? firstSweepAt;
            await using (var db = CreateContext())
            {
                var watermark = await db.DashboardRollupWatermarks.SingleAsync(
                    x => x.OrganizationId == _orgAId && x.RollupKind == DashboardRollupKind.EmployeeDaily.ToString());
                firstSweepAt = watermark.LastWideSweepUtc;
                Assert.NotNull(firstSweepAt);
            }

            // Same UTC day, same hour, called again immediately - must not re-run (idempotent: the
            // watermark's LastWideSweepUtc must not move).
            await refreshService.RefreshRecentWindowAsync(CancellationToken.None);

            await using (var db = CreateContext())
            {
                var watermark = await db.DashboardRollupWatermarks.SingleAsync(
                    x => x.OrganizationId == _orgAId && x.RollupKind == DashboardRollupKind.EmployeeDaily.ToString());
                Assert.Equal(firstSweepAt, watermark.LastWideSweepUtc);
            }
        }

        // --- distributed lock -----------------------------------------------------------------------

        [Fact]
        public async Task RefreshRecentWindowAsync_AnotherInstanceHoldsTheLock_SkipsWithoutDoingWork()
        {
            await using var lockHolderConnection = new SqlConnection(ConnectionString);
            await lockHolderConnection.OpenAsync();

            await using (var acquireCommand = lockHolderConnection.CreateCommand())
            {
                acquireCommand.CommandText = "sp_getapplock";
                acquireCommand.CommandType = CommandType.StoredProcedure;
                acquireCommand.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = "DashboardRollupRefresh" });
                acquireCommand.Parameters.Add(new SqlParameter("@LockMode", SqlDbType.VarChar, 32) { Value = "Exclusive" });
                acquireCommand.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Session" });
                acquireCommand.Parameters.Add(new SqlParameter("@LockTimeout", SqlDbType.Int) { Value = 0 });
                var returnValue = new SqlParameter("@ReturnValue", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                acquireCommand.Parameters.Add(returnValue);
                await acquireCommand.ExecuteNonQueryAsync();
                Assert.True((int)returnValue.Value >= 0);
            }

            try
            {
                var result = await CreateRefreshService().RefreshRecentWindowAsync(CancellationToken.None);

                Assert.False(result.LockAcquired);
                Assert.Equal(0, result.OrganizationsProcessed);
                Assert.Equal(0, result.HourlyRowsWritten);
                Assert.Equal(0, result.EmployeeDailyRowsWritten);
            }
            finally
            {
                await using var releaseCommand = lockHolderConnection.CreateCommand();
                releaseCommand.CommandText = "sp_releaseapplock";
                releaseCommand.CommandType = CommandType.StoredProcedure;
                releaseCommand.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = "DashboardRollupRefresh" });
                releaseCommand.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Session" });
                await releaseCommand.ExecuteNonQueryAsync();
            }
        }

        // --- UseRollup flag routing (RollupAwareDlpDashboardQueryService) ---------------------------

        private sealed class FakeOptionsMonitor<T> : IOptionsMonitor<T>
        {
            public FakeOptionsMonitor(T value) => CurrentValue = value;
            public T CurrentValue { get; set; }
            public T Get(string? name) => CurrentValue;
            public IDisposable OnChange(Action<T, string> listener) => NullDisposable.Instance;

            private sealed class NullDisposable : IDisposable
            {
                public static readonly NullDisposable Instance = new();
                public void Dispose() { }
            }
        }

        [Fact]
        public async Task RollupAwareDlpDashboardQueryService_UseRollupFlag_RoutesToTheCorrectImplementationBothWays()
        {
            var hourFloor = FloorToHour(DateTimeOffset.UtcNow);

            await InsertEventAsync(_orgAId, _deviceAId, null, hourFloor.AddHours(-1), hourFloor.AddHours(-1), "Block");
            await CreateRefreshService().RefreshRecentWindowAsync(CancellationToken.None);

            var range = new DashboardRange(DashboardRangeKind.TwentyFourHours, "24h", hourFloor.AddHours(-24), hourFloor);

            var optionsMonitor = new FakeOptionsMonitor<DashboardRollupOptions>(DefaultRollupOptions());
            var decorator = new RollupAwareDlpDashboardQueryService(CreateRawQueryService(), CreateRollupQueryService(), optionsMonitor);

            optionsMonitor.CurrentValue.UseRollup = false;
            var viaDecoratorRaw = await decorator.GetKpisAsync(_orgAId, range, CancellationToken.None);
            var raw = await CreateRawQueryService().GetKpisAsync(_orgAId, range, CancellationToken.None);
            Assert.Equal(raw.Totals, viaDecoratorRaw.Totals);

            optionsMonitor.CurrentValue.UseRollup = true;
            var viaDecoratorRollup = await decorator.GetKpisAsync(_orgAId, range, CancellationToken.None);
            var rollup = await CreateRollupQueryService().GetKpisAsync(_orgAId, range, CancellationToken.None);
            Assert.Equal(rollup.Totals, viaDecoratorRollup.Totals);

            // summary/status/recent-events must never be affected by the flag - always the raw path.
            var statusViaDecorator = await decorator.GetStatusAsync(_orgAId, CancellationToken.None);
            var statusRaw = await CreateRawQueryService().GetStatusAsync(_orgAId, CancellationToken.None);
            Assert.Equal(statusRaw, statusViaDecorator);
        }

        // --- AuditRetentionWorker's rollup-watermark purge guard -------------------------------------

        private sealed class SingleScopeFactory : IServiceScopeFactory
        {
            private readonly DLPSystemContext _db;
            public SingleScopeFactory(DLPSystemContext db) => _db = db;
            public IServiceScope CreateScope() => new SingleScope(_db);

            private sealed class SingleScope : IServiceScope
            {
                public SingleScope(DLPSystemContext db) => ServiceProvider = new SingleProvider(db);
                public IServiceProvider ServiceProvider { get; }
                public void Dispose() { }
            }

            private sealed class SingleProvider : IServiceProvider
            {
                private readonly DLPSystemContext _db;
                public SingleProvider(DLPSystemContext db) => _db = db;
                public object? GetService(Type serviceType) => serviceType == typeof(DLPSystemContext) ? _db : null;
            }
        }

        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public FakeHostEnvironment(string contentRootPath) => ContentRootPath = contentRootPath;
            public string EnvironmentName { get; set; } = "Development";
            public string ApplicationName { get; set; } = "DashboardRollupRefreshServiceTests";
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }

        [Fact]
        public async Task AuditRetentionWorker_NeverPurgesRawEventsNewerThanTheRollupWatermark()
        {
            var nowUtc = DateTimeOffset.UtcNow;

            // RetentionDays=1 -> cutoff = now-1 day; both events below are older than that, so
            // without the rollup guard both would normally be purge-eligible.
            var safeEventOccurredAt = nowUtc.AddDays(-10);       // older than the watermark - purgeable
            var protectedEventOccurredAt = nowUtc.AddDays(-2);   // newer than the watermark - must survive

            await InsertEventAsync(_orgAId, _deviceAId, null, safeEventOccurredAt, safeEventOccurredAt, "Allow");
            await InsertEventAsync(_orgAId, _deviceAId, null, protectedEventOccurredAt, protectedEventOccurredAt, "Allow");

            var watermarkThroughUtc = nowUtc.AddDays(-5); // strictly between the two events' OccurredAtUtc
            await using (var db = CreateContext())
            {
                db.DashboardRollupWatermarks.AddRange(
                    new DashboardRollupWatermark
                    {
                        OrganizationId = _orgAId,
                        RollupKind = DashboardRollupKind.HourlyOccurred.ToString(),
                        LastComputedThroughUtc = watermarkThroughUtc,
                        UpdatedAtUtc = nowUtc
                    },
                    new DashboardRollupWatermark
                    {
                        OrganizationId = _orgAId,
                        RollupKind = DashboardRollupKind.EmployeeDaily.ToString(),
                        LastComputedThroughUtc = watermarkThroughUtc,
                        UpdatedAtUtc = nowUtc
                    });
                await db.SaveChangesAsync();
            }

            var retentionOptions = new AuditRetentionOptions
            {
                RetentionDays = 1,
                ArchiveDirectory = "dashboard-rollup-tests-archive",
                BatchSize = 1000
            };

            await using var retentionDb = CreateContext();
            var worker = new AuditRetentionWorker(
                new SingleScopeFactory(retentionDb),
                new FakeHostEnvironment(Path.GetTempPath()),
                Options.Create(retentionOptions),
                NullLogger<AuditRetentionWorker>.Instance);

            var method = typeof(AuditRetentionWorker).GetMethod("RunRetentionCycleAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await (Task)method.Invoke(worker, new object[] { CancellationToken.None })!;

            await using var verifyDb = CreateContext();
            var remainingOccurredTimes = await verifyDb.AuditEvents
                .Where(x => x.OrganizationId == _orgAId)
                .Select(x => x.OccurredAtUtc)
                .ToListAsync();

            Assert.DoesNotContain(remainingOccurredTimes, t => t == safeEventOccurredAt);
            Assert.Contains(remainingOccurredTimes, t => t == protectedEventOccurredAt);
        }
    }
}
