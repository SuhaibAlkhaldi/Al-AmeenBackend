using DLPManagementSystem.DTO.AgentFileInventory;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DLPManagementSystem.Tests.Service
{
    // Covers a real race found live 2026-09-27 during the "File Tracking" plan's Phase 1/4 rollout:
    // two independent detection paths for the agent (a real-time rename report and an unrelated
    // reconciliation-scan "Created" report) can both describe the same underlying file event and reach
    // AgentFileInventoryService in two overlapping HTTP calls milliseconds apart. Each call's own
    // activeByPath snapshot (loaded once at the top of ApplyFileInventoryBatchAsync) can be stale
    // relative to the OTHER call's just-committed row, so both could conclude "this path has never been
    // seen" and each create its own competing FileId - exactly the same shape of bug
    // DeviceUserAssignmentConcurrencyTests.cs documents for AssignDeviceAsync, fixed here the same way:
    // UpsertActiveRecordAsync's own DB re-check narrows the window, and a final SaveChangesAsync catch
    // is the last-resort safety net for whatever residual race that re-check's own read-then-act gap
    // cannot fully close.
    //
    // Same reasoning as DeviceUserAssignmentConcurrencyTests/PolicyVersionServiceConcurrencyTests for
    // using the real local dev SQL Server database instead of TestDbContextFactory's InMemory provider:
    // UX_FileInventory_Active_DeviceId_FilePath is a real database-level constraint InMemory doesn't
    // enforce at all.
    public sealed class AgentFileInventoryServiceConcurrencyTests : IAsyncLifetime
    {
        private const string ConnectionString =
            "Server=localhost,1433;Database=DLPSystem;User Id=ameen_dev_login;Password=AmeenDev_2026!;TrustServerCertificate=True;Connect Timeout=60;";

        private Guid _organizationId;
        private Guid _deviceId;

        private static DLPSystemContext CreateContext() =>
            new(new DbContextOptionsBuilder<DLPSystemContext>().UseSqlServer(ConnectionString).Options);

        public async Task InitializeAsync()
        {
            await using var db = CreateContext();

            var activeDeviceStatusId = await db.DeviceStatuses.Where(x => x.Name == "Active").Select(x => x.Id).SingleAsync();

            _organizationId = Guid.NewGuid();
            db.Organizations.Add(new Organization
            {
                Id = _organizationId,
                Name = "File Inventory Concurrency Test Org",
                Code = $"FICT-{_organizationId:N}"[..20],
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            _deviceId = Guid.NewGuid();
            db.Devices.Add(new Device
            {
                Id = _deviceId,
                OrganizationId = _organizationId,
                DeviceKey = Guid.NewGuid().ToString("N"),
                MachineName = "CONC-TEST-FILE-DEVICE",
                StatusId = activeDeviceStatusId,
                CurrentPolicyVersion = 0,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await using var db = CreateContext();
            // FK order: Alerts -> AuditEvents; FileVersionHistories, FileInventoryRecords -> Device, Organization.
            await db.Alerts.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.AuditEvents.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.FileVersionHistories.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.FileInventoryRecords.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Devices.Where(x => x.OrganizationId == _organizationId).ExecuteDeleteAsync();
            await db.Organizations.Where(x => x.Id == _organizationId).ExecuteDeleteAsync();
        }

        [Fact]
        public async Task ApplyFileInventoryBatchAsync_ConcurrentRenameAndCreatedForSamePath_OnlyOneActiveRecordSurvives()
        {
            const string oldPath = @"C:\Concurrency\[Secret] concurrency-test.txt";
            const string newPath = @"C:\Concurrency\[Public] concurrency-test.txt";

            // Setup: the file already exists at oldPath, Secret - not part of the race itself.
            await using (var setupDb = CreateContext())
            {
                var setupService = new AgentFileInventoryService(setupDb, NullLogger<AgentFileInventoryService>.Instance, new FileVersionTextProtector(new EphemeralDataProtectionProvider()));
                await setupService.ApplyFileInventoryBatchAsync(_organizationId, _deviceId, new AgentFileInventoryBatchRequestDto
                {
                    TenantId = _organizationId,
                    DeviceId = _deviceId,
                    AgentVersion = "test",
                    SyncKind = FileInventorySyncKinds.Incremental,
                    Changes = new List<FileInventoryChangeDto> { new() {
                        ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Created, FilePath = oldPath,
                        FileHash = "CONC_V1", Extension = "txt", SizeBytes = 100, ClassificationTier = "Secret",
                        Provenance = "SelfCreated", IsProtected = true, OccurredAtUtc = DateTimeOffset.UtcNow
                    }}
                });
            }

            // The race: a real Renamed report (correlated oldPath -> newPath) and an independent
            // Created report for newPath directly, dispatched concurrently on separate DbContexts -
            // exactly the two overlapping detection paths observed live.
            await using var dbRenamed = CreateContext();
            var serviceRenamed = new AgentFileInventoryService(dbRenamed, NullLogger<AgentFileInventoryService>.Instance, new FileVersionTextProtector(new EphemeralDataProtectionProvider()));
            var taskRenamed = serviceRenamed.ApplyFileInventoryBatchAsync(_organizationId, _deviceId, new AgentFileInventoryBatchRequestDto
            {
                TenantId = _organizationId,
                DeviceId = _deviceId,
                AgentVersion = "test",
                SyncKind = FileInventorySyncKinds.Incremental,
                Changes = new List<FileInventoryChangeDto> { new() {
                    ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Renamed, FilePath = newPath,
                    OldFilePath = oldPath, FileHash = "CONC_V2", Extension = "txt", SizeBytes = 100,
                    ClassificationTier = "Public", Provenance = "SelfCreated", IsProtected = false, OccurredAtUtc = DateTimeOffset.UtcNow
                }}
            });

            await using var dbCreated = CreateContext();
            var serviceCreated = new AgentFileInventoryService(dbCreated, NullLogger<AgentFileInventoryService>.Instance, new FileVersionTextProtector(new EphemeralDataProtectionProvider()));
            var taskCreated = serviceCreated.ApplyFileInventoryBatchAsync(_organizationId, _deviceId, new AgentFileInventoryBatchRequestDto
            {
                TenantId = _organizationId,
                DeviceId = _deviceId,
                AgentVersion = "test",
                SyncKind = FileInventorySyncKinds.Incremental,
                Changes = new List<FileInventoryChangeDto> { new() {
                    ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Created, FilePath = newPath,
                    FileHash = "CONC_V2", Extension = "txt", SizeBytes = 100, ClassificationTier = "Public",
                    Provenance = "SelfCreated", IsProtected = false, OccurredAtUtc = DateTimeOffset.UtcNow
                }}
            });

            var results = await Task.WhenAll(taskRenamed, taskCreated);

            // Neither call should surface an unhandled exception - a caller that genuinely lost the
            // residual race must get the clean, handled "please retry" response, not a raw 500.
            const string retryMessage = "A concurrent update for the same file path was applied first. Please retry this batch.";
            Assert.All(results, r => Assert.True(
                r.Success || r.MessageEn == retryMessage,
                $"Unexpected failure: {r.MessageEn}"));

            // Realistic completion of the scenario: a batch that comes back with Success=false is
            // exactly what the agent's own existing outbox/retry mechanism is already built to resend
            // later (the same reliance DeviceService.AssignDeviceAsync's analogous "please refresh and
            // try again" places on its caller) - simulate that retry here for whichever side lost.
            if (results[0].MessageEn == retryMessage)
            {
                await using var retryDb = CreateContext();
                var retryService = new AgentFileInventoryService(retryDb, NullLogger<AgentFileInventoryService>.Instance, new FileVersionTextProtector(new EphemeralDataProtectionProvider()));
                var retryResult = await retryService.ApplyFileInventoryBatchAsync(_organizationId, _deviceId, new AgentFileInventoryBatchRequestDto
                {
                    TenantId = _organizationId, DeviceId = _deviceId, AgentVersion = "test", SyncKind = FileInventorySyncKinds.Incremental,
                    Changes = new List<FileInventoryChangeDto> { new() {
                        ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Renamed, FilePath = newPath,
                        OldFilePath = oldPath, FileHash = "CONC_V2", Extension = "txt", SizeBytes = 100,
                        ClassificationTier = "Public", Provenance = "SelfCreated", IsProtected = false, OccurredAtUtc = DateTimeOffset.UtcNow
                    }}
                });
                Assert.True(retryResult.Success, $"Retry of the losing batch should succeed: {retryResult.MessageEn}");
            }

            await using var verifyDb = CreateContext();
            var activeRecords = await verifyDb.FileInventoryRecords
                .Where(x => x.DeviceId == _deviceId && x.FilePath == newPath && !x.IsDeleted)
                .ToListAsync();

            // The core guarantee: exactly one active row for the path both calls raced on - never two
            // competing FileIds, regardless of which call's report happened to commit first.
            Assert.Single(activeRecords);
            Assert.Equal("Public", activeRecords[0].ClassificationTier);

            // The real transition (Secret -> Public) must be visible somewhere in this FileId's
            // history - not silently lost to a blind "Genesis" row, regardless of arrival order.
            var history = await verifyDb.FileVersionHistories
                .Where(x => x.FileId == activeRecords[0].FileId)
                .OrderBy(x => x.ObservedAtUtc)
                .ToListAsync();

            Assert.Contains(history, h => h.OldClassificationTier == "Secret" && h.NewClassificationTier == "Public");
        }

        [Fact]
        public async Task ApplyFileInventoryBatchAsync_DuplicateRenamedReportsInSameBatch_OnlyOneActiveRecordSurvivesWithCorrectHistory()
        {
            // Covers a second, distinct bug found live 2026-09-27 immediately after fixing the
            // cross-batch race above: this one is NOT a concurrency race at all - it is the agent
            // reporting the exact same rename twice within ONE batch (both changes share the same
            // nowUtc server-side, proving they were processed sequentially in a single
            // ApplyFileInventoryBatchAsync call, not two overlapping HTTP calls). The second report
            // used to be blind to the first's own in-memory activeByPath update (Renamed unconditionally
            // skipped that lookup for the target path) and created a second, competing FileId with a
            // blind Genesis, silently orphaning the first's correctly-captured Secret->Public transition
            // onto whichever FileId lost the "stays active" pick.
            const string oldPath = @"C:\Concurrency\[Secret] duplicate-in-batch-test.txt";
            const string newPath = @"C:\Concurrency\[Public] duplicate-in-batch-test.txt";

            await using (var setupDb = CreateContext())
            {
                var setupService = new AgentFileInventoryService(setupDb, NullLogger<AgentFileInventoryService>.Instance, new FileVersionTextProtector(new EphemeralDataProtectionProvider()));
                await setupService.ApplyFileInventoryBatchAsync(_organizationId, _deviceId, new AgentFileInventoryBatchRequestDto
                {
                    TenantId = _organizationId, DeviceId = _deviceId, AgentVersion = "test", SyncKind = FileInventorySyncKinds.Incremental,
                    Changes = new List<FileInventoryChangeDto> { new() {
                        ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Created, FilePath = oldPath,
                        FileHash = "DUP_V1", Extension = "txt", SizeBytes = 100, ClassificationTier = "Secret",
                        Provenance = "SelfCreated", IsProtected = true, OccurredAtUtc = DateTimeOffset.UtcNow
                    }}
                });
            }

            await using var db = CreateContext();
            var service = new AgentFileInventoryService(db, NullLogger<AgentFileInventoryService>.Instance, new FileVersionTextProtector(new EphemeralDataProtectionProvider()));

            // ONE batch, TWO identical Renamed changes for the same rename - the exact shape observed live.
            var duplicateRenameChange = new Func<FileInventoryChangeDto>(() => new FileInventoryChangeDto
            {
                ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Renamed, FilePath = newPath,
                OldFilePath = oldPath, FileHash = "DUP_V2", Extension = "txt", SizeBytes = 100,
                ClassificationTier = "Public", Provenance = "SelfCreated", IsProtected = false, OccurredAtUtc = DateTimeOffset.UtcNow
            });

            var result = await service.ApplyFileInventoryBatchAsync(_organizationId, _deviceId, new AgentFileInventoryBatchRequestDto
            {
                TenantId = _organizationId, DeviceId = _deviceId, AgentVersion = "test", SyncKind = FileInventorySyncKinds.Incremental,
                Changes = new List<FileInventoryChangeDto> { duplicateRenameChange(), duplicateRenameChange() }
            });

            Assert.True(result.Success, $"Batch should succeed cleanly: {result.MessageEn}");
            Assert.Equal(2, result.Data!.AcceptedChangeIds.Count);

            await using var verifyDb = CreateContext();
            var activeRecords = await verifyDb.FileInventoryRecords
                .Where(x => x.DeviceId == _deviceId && x.FilePath == newPath && !x.IsDeleted)
                .ToListAsync();

            // The core guarantee: exactly one active row, even though the batch reported the rename twice.
            Assert.Single(activeRecords);
            Assert.Equal("Public", activeRecords[0].ClassificationTier);

            // The real transition must live on the SAME (surviving) FileId - not orphaned onto a
            // different, now-deleted one, and not duplicated into two history rows either (the second,
            // duplicate report must be absorbed as a no-op once it finds the first's own update).
            var history = await verifyDb.FileVersionHistories
                .Where(x => x.FileId == activeRecords[0].FileId)
                .OrderBy(x => x.ObservedAtUtc)
                .ToListAsync();

            Assert.Single(history);
            Assert.Equal("Secret", history[0].OldClassificationTier);
            Assert.Equal("Public", history[0].NewClassificationTier);
        }

        [Fact]
        public async Task ApplyFileInventoryBatchAsync_TwoDifferentRenamesConvergingOnSamePathInOneBatch_NoTransitionIsSilentlyLost()
        {
            // Covers a third, distinct bug found live 2026-09-27 during a real end-to-end retest of the
            // classification-downgrade feature (suhaib2.txt): unlike the duplicate-report bug above, this
            // is TWO REAL, DIFFERENT renames - the classification-tagging rename and the content
            // watermarker's own tmp-swap rename (its OldFilePath is a temp file that was never itself an
            // active record) - both landing in ONE batch and converging on the SAME final path. Whichever
            // one is processed second used to blindly apply its OWN carriedOverPriorState (its own
            // renamed-from row's values) as "old", even though `record` at the target path had ALREADY
            // been updated by the first one earlier in this same loop - silently regressing the record's
            // fields back to a stale value with no history row flagging it (the stale-vs-fresher values
            // happened to compare equal), which is exactly what made the real suhaib2.txt File History
            // page show a blind "Genesis" and hide that the file had ever been Secret.
            const string barePath = @"C:\Concurrency\converging-rename-test.txt";
            const string taggedPath = @"C:\Concurrency\[Secret] converging-rename-test.txt";
            const string tmpPath = @"C:\Concurrency\[Secret] converging-rename-test.txt.tmp";

            await using var db = CreateContext();
            var service = new AgentFileInventoryService(db, NullLogger<AgentFileInventoryService>.Instance, new FileVersionTextProtector(new EphemeralDataProtectionProvider()));

            // ONE batch: Created(bare) -> Renamed(tmp->tagged, unrelated/never-tracked old path,
            // processed FIRST) -> Renamed(bare->tagged, a real carried-over prior state, processed
            // SECOND and converging on the same target the first one already wrote to).
            var result = await service.ApplyFileInventoryBatchAsync(_organizationId, _deviceId, new AgentFileInventoryBatchRequestDto
            {
                TenantId = _organizationId, DeviceId = _deviceId, AgentVersion = "test", SyncKind = FileInventorySyncKinds.Incremental,
                Changes = new List<FileInventoryChangeDto>
                {
                    new() {
                        ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Created, FilePath = barePath,
                        FileHash = "CONV_BARE", Extension = "txt", SizeBytes = 100, ClassificationTier = "Secret",
                        Provenance = "SelfCreated", IsProtected = false, OccurredAtUtc = DateTimeOffset.UtcNow
                    },
                    new() {
                        ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Renamed, FilePath = taggedPath,
                        OldFilePath = tmpPath, FileHash = "CONV_WATERMARKED", Extension = "txt", SizeBytes = 140, ClassificationTier = "Secret",
                        Provenance = "SelfCreated", IsProtected = false, OccurredAtUtc = DateTimeOffset.UtcNow
                    },
                    new() {
                        ChangeId = Guid.NewGuid(), ChangeType = FileInventoryChangeTypes.Renamed, FilePath = taggedPath,
                        OldFilePath = barePath, FileHash = "CONV_BARE", Extension = "txt", SizeBytes = 100, ClassificationTier = "Secret",
                        Provenance = "SelfCreated", IsProtected = false, OccurredAtUtc = DateTimeOffset.UtcNow
                    }
                }
            });

            Assert.True(result.Success, $"Batch should succeed cleanly: {result.MessageEn}");
            Assert.Equal(3, result.Data!.AcceptedChangeIds.Count);

            await using var verifyDb = CreateContext();
            var activeRecords = await verifyDb.FileInventoryRecords
                .Where(x => x.DeviceId == _deviceId && x.FilePath == taggedPath && !x.IsDeleted)
                .ToListAsync();

            // Exactly one active row, even though two different renames converged on it.
            Assert.Single(activeRecords);

            var history = await verifyDb.FileVersionHistories
                .Where(x => x.FileId == activeRecords[0].FileId)
                .OrderBy(x => x.ObservedAtUtc)
                .ToListAsync();

            // The core guarantee: the second rename's own "old" side must be the target path's
            // just-applied state (CONV_WATERMARKED), never a stale replay of its own renamed-from row
            // that ignores what this same batch already wrote - so the file's real Secret content
            // history is never silently erased into a blind, lineage-free "Genesis".
            Assert.Contains(history, h => h.OldFileHash == "CONV_WATERMARKED");
        }
    }
}
