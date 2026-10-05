using System.Reflection;
using System.Security.Cryptography;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DLPManagementSystem.Tests.Service
{
    public class FileVersionDiffServiceTests : IDisposable
    {
        private static readonly Guid OrgA = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
        private static readonly Guid OrgB = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
        private static readonly Guid DeviceA = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
        private static readonly Guid FileA = Guid.Parse("00000000-0000-0000-0000-0000000000f1");
        private static readonly Guid UserA = Guid.Parse("00000000-0000-0000-0000-0000000000e1");

        private static readonly DateTimeOffset T1 = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset T2 = new(2026, 10, 5, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset T3 = new(2026, 10, 5, 11, 0, 0, TimeSpan.Zero);

        private readonly DLPSystemContext _db;
        private readonly FileVersionTextProtector _protector = new(new EphemeralDataProtectionProvider());
        private readonly FileVersionDiffService _service;

        public FileVersionDiffServiceTests()
        {
            var options = new DbContextOptionsBuilder<DLPSystemContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new DLPSystemContext(options);
            _db.Devices.Add(new Device
            {
                Id = DeviceA,
                OrganizationId = OrgA,
                DeviceKey = "device-a-key",
                MachineName = "PC-ONE",
                CreatedAtUtc = T1
            });
            _db.SaveChanges();

            _service = new FileVersionDiffService(_db, _protector, NullLogger<FileVersionDiffService>.Instance);
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        [Fact]
        public async Task ContentChange_ShowsAddedAndRemovedLines()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            AddVersion(first, OrgA, T1, "a\nb\nc");
            AddVersion(second, OrgA, T2, "a\nB\nc\nd");
            await _db.SaveChangesAsync();

            var response = await _service.GetDiffAsync(OrgA, UserA, second, CancellationToken.None);

            Assert.True(response.Success);
            var diff = response.Data!;
            Assert.True(diff.IsAvailable);
            Assert.Equal(first, diff.PreviousVersionId);
            Assert.False(diff.IsFirstVersion);
            Assert.Equal(2, diff.AddedLineCount);
            Assert.Equal(1, diff.RemovedLineCount);
            Assert.Contains(diff.Lines, line => line.Kind == "removed" && line.Text == "b" && line.OldLineNumber == 2);
            Assert.Contains(diff.Lines, line => line.Kind == "added" && line.Text == "d" && line.NewLineNumber == 4);
        }

        [Fact]
        public async Task FirstVersion_ComparesAgainstEmptyText()
        {
            var only = Guid.NewGuid();
            AddVersion(only, OrgA, T1, "x\ny");
            await _db.SaveChangesAsync();

            var diff = (await _service.GetDiffAsync(OrgA, UserA, only, CancellationToken.None)).Data!;

            Assert.True(diff.IsAvailable);
            Assert.True(diff.IsFirstVersion);
            Assert.Null(diff.PreviousVersionId);
            Assert.Equal(2, diff.AddedLineCount);
            Assert.Equal(0, diff.RemovedLineCount);
        }

        [Fact]
        public async Task VersionFromAnotherOrganization_IsNotFound()
        {
            var id = Guid.NewGuid();
            AddVersion(id, OrgB, T1, "secret");
            await _db.SaveChangesAsync();

            var response = await _service.GetDiffAsync(OrgA, UserA, id, CancellationToken.None);

            Assert.False(response.Success);
            Assert.Null(response.Data);
        }

        [Fact]
        public async Task VersionWithoutCapturedText_ReportsTextNotAvailable()
        {
            var id = Guid.NewGuid();
            AddVersion(id, OrgA, T1, text: null);
            await _db.SaveChangesAsync();

            var diff = (await _service.GetDiffAsync(OrgA, UserA, id, CancellationToken.None)).Data!;

            Assert.False(diff.IsAvailable);
            Assert.Equal("text_not_available", diff.UnavailableReason);
        }

        [Fact]
        public async Task PreviousVersionWithoutCapturedText_ReportsPreviousNotAvailable()
        {
            var previous = Guid.NewGuid();
            var current = Guid.NewGuid();
            AddVersion(previous, OrgA, T1, text: null);
            AddVersion(current, OrgA, T2, "now captured");
            await _db.SaveChangesAsync();

            var diff = (await _service.GetDiffAsync(OrgA, UserA, current, CancellationToken.None)).Data!;

            Assert.False(diff.IsAvailable);
            Assert.Equal("previous_text_not_available", diff.UnavailableReason);
        }

        [Fact]
        public async Task ClassificationOnlyRow_ReportsNotAContentChange()
        {
            var id = Guid.NewGuid();
            _db.FileVersionHistories.Add(Version(id, OrgA, T1, contentChanged: false));
            await _db.SaveChangesAsync();

            var diff = (await _service.GetDiffAsync(OrgA, UserA, id, CancellationToken.None)).Data!;

            Assert.False(diff.IsAvailable);
            Assert.Equal("not_a_content_change", diff.UnavailableReason);
        }

        [Fact]
        public async Task LongUnchangedRuns_AreFoldedIntoGaps()
        {
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            var oldText = string.Join("\n", Enumerable.Range(1, 20).Select(i => "line" + i));
            var newText = oldText.Replace("line10", "changed10");
            AddVersion(first, OrgA, T1, oldText);
            AddVersion(second, OrgA, T2, newText);
            await _db.SaveChangesAsync();

            var diff = (await _service.GetDiffAsync(OrgA, UserA, second, CancellationToken.None)).Data!;

            // Three unchanged lines of context either side of the change; the rest is folded into two gaps.
            var kinds = diff.Lines.Select(line => line.Kind).ToArray();
            Assert.Equal(new[] { "gap", "equal", "equal", "equal", "removed", "added", "equal", "equal", "equal", "gap" }, kinds);
            Assert.Equal(6, diff.Lines.First().SkippedLineCount);
            Assert.Equal(7, diff.Lines.Last().SkippedLineCount);
            Assert.Equal(10, diff.Lines.Single(line => line.Kind == "removed").OldLineNumber);
            Assert.Equal(10, diff.Lines.Single(line => line.Kind == "added").NewLineNumber);
        }

        [Fact]
        public async Task StoredText_IsNotPlaintext()
        {
            var id = Guid.NewGuid();
            AddVersion(id, OrgA, T1, "very sensitive plan");
            await _db.SaveChangesAsync();

            var stored = await _db.FileVersionContents.AsNoTracking().SingleAsync(x => x.FileVersionHistoryId == id);

            Assert.DoesNotContain("very sensitive plan", stored.EncryptedText);
            Assert.Equal("very sensitive plan".Length, stored.CharCount);
        }

        [Fact]
        public async Task LaterVersion_IgnoresFullCopyRowsAsPrevious()
        {
            var change = Guid.NewGuid();
            var copy = Guid.NewGuid();
            var latest = Guid.NewGuid();
            AddVersion(change, OrgA, T1, "one");
            _db.FileVersionHistories.Add(Version(copy, OrgA, T2, contentChanged: false, eventType: "FullCopy"));
            AddVersion(latest, OrgA, T3, "one\ntwo");
            await _db.SaveChangesAsync();

            var diff = (await _service.GetDiffAsync(OrgA, UserA, latest, CancellationToken.None)).Data!;

            Assert.Equal(change, diff.PreviousVersionId);
            Assert.Equal(1, diff.AddedLineCount);
        }

        [Fact]
        public async Task RetaggedFile_ComparesAgainstPreviousVersionOfSameLineage()
        {
            // A classification tag change renames the file and gives it a new FileId. The diff must still compare
            // against the version from before the rename, the same way the File History timeline does.
            var beforeRetag = Guid.NewGuid();
            var afterRetag = Guid.NewGuid();
            var before = Guid.NewGuid();
            var after = Guid.NewGuid();
            AddRecord(beforeRetag, "[Secret] plan.txt", T1);
            AddRecord(afterRetag, "[Very Secret] plan.txt", T2);
            AddVersionForFile(before, beforeRetag, T1, "a\nb");
            AddVersionForFile(after, afterRetag, T2, "a\nB");
            await _db.SaveChangesAsync();

            var diff = (await _service.GetDiffAsync(OrgA, UserA, after, CancellationToken.None)).Data!;

            Assert.True(diff.IsAvailable);
            Assert.Equal(before, diff.PreviousVersionId);
            Assert.Equal(1, diff.AddedLineCount);
            Assert.Equal(1, diff.RemovedLineCount);
            Assert.Equal("[Very Secret] plan.txt", diff.FileName);
        }

        private void AddRecord(Guid fileId, string fileName, DateTimeOffset seenAtUtc)
        {
            _db.FileInventoryRecords.Add(new FileInventoryRecord
            {
                FileId = fileId,
                OrganizationId = OrgA,
                DeviceId = DeviceA,
                FilePath = @"C:\Docs\" + fileName,
                FileName = fileName,
                SizeBytes = 1,
                ClassificationTier = "Secret",
                Provenance = "SelfCreated",
                LastScanAtUtc = seenAtUtc,
                FirstSeenAtUtc = seenAtUtc,
                UpdatedAtUtc = seenAtUtc,
                IsDeleted = false
            });
        }

        private void AddVersionForFile(Guid id, Guid fileId, DateTimeOffset observedAtUtc, string text)
        {
            _db.FileVersionHistories.Add(new FileVersionHistory
            {
                Id = id,
                FileId = fileId,
                OrganizationId = OrgA,
                ObservedAtUtc = observedAtUtc,
                NewFileHash = "hash",
                NewClassificationTier = "Secret",
                ContentChanged = true,
                EventType = "Change",
                DeviceId = DeviceA
            });
            _db.FileVersionContents.Add(new FileVersionContent
            {
                FileVersionHistoryId = id,
                OrganizationId = OrgA,
                FileId = fileId,
                CapturedAtUtc = observedAtUtc,
                CharCount = text.Length,
                EncryptedText = _protector.Protect(text)
            });
        }

        private void AddVersion(Guid id, Guid organization, DateTimeOffset observedAtUtc, string? text = "")
        {
            _db.FileVersionHistories.Add(Version(id, organization, observedAtUtc));
            if (text is not null)
            {
                _db.FileVersionContents.Add(new FileVersionContent
                {
                    FileVersionHistoryId = id,
                    OrganizationId = organization,
                    FileId = FileA,
                    CapturedAtUtc = observedAtUtc,
                    CharCount = text.Length,
                    EncryptedText = _protector.Protect(text)
                });
            }
        }

        private static FileVersionHistory Version(Guid id, Guid organization, DateTimeOffset observedAtUtc, bool contentChanged = true, string eventType = "Change") => new()
        {
            Id = id,
            FileId = FileA,
            OrganizationId = organization,
            ObservedAtUtc = observedAtUtc,
            NewFileHash = "hash",
            NewClassificationTier = "Secret",
            ContentChanged = contentChanged,
            EventType = eventType,
            DeviceId = DeviceA
        };
    }

    public class FileVersionTextProtectorTests
    {
        [Fact]
        public void RoundTrip_RestoresText()
        {
            var protector = new FileVersionTextProtector(new EphemeralDataProtectionProvider());

            var encrypted = protector.Protect("contract terms");

            Assert.Equal("contract terms", protector.Unprotect(encrypted));
        }

        [Fact]
        public void PayloadFromAnotherFeature_CannotBeOpenedAsVersionText()
        {
            // Same key ring, different purpose string: a FileKeys payload must not unprotect as version text.
            var provider = new EphemeralDataProtectionProvider();
            var otherFeature = provider.CreateProtector("CompanyDlp.FileKeys").Protect("key material");
            var protector = new FileVersionTextProtector(provider);

            Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(otherFeature));
        }
    }

    public class FileVersionDiffControllerTests
    {
        [Fact]
        public void Endpoint_IsLimitedToSuperAdminAndSecurityAdmin()
        {
            var authorize = typeof(Controllers.FileVersionDiffController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(authorize);
            Assert.Equal("SuperAdmin,SecurityAdmin", authorize!.Roles);
        }
    }
}
