using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileTransfer;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DLPManagementSystem.Tests.Service
{
    public class FileTransferEventServiceTests : IDisposable
    {
        private static readonly Guid OrgA = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
        private static readonly Guid OrgB = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
        private static readonly Guid DeviceA = Guid.Parse("00000000-0000-0000-0000-0000000000d1");
        private static readonly Guid DeviceB = Guid.Parse("00000000-0000-0000-0000-0000000000d2");
        private static readonly Guid FileA = Guid.Parse("00000000-0000-0000-0000-0000000000f1");
        private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string OtherHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

        private readonly DLPSystemContext _db;
        private readonly FileTransferEventService _service;

        public FileTransferEventServiceTests()
        {
            var options = new DbContextOptionsBuilder<DLPSystemContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new DLPSystemContext(options);

            _db.Devices.Add(Device(DeviceA, OrgA, "device-a-key", "PC-A"));
            _db.Devices.Add(Device(DeviceB, OrgA, "device-b-key", "PC-B"));
            _db.FileInventoryRecords.Add(Record(DeviceA, OrgA, "[Secret] plan.pdf", Hash, 10, "Secret"));
            _db.FileInventoryRecords.Add(Record(DeviceB, OrgA, "[Secret] other.pdf", OtherHash, 20, "Secret"));
            _db.FileInventoryRecords.Add(Record(DeviceA, OrgA, "[Public] notes.txt", OtherHash, 5, "Public"));
            _db.SaveChanges();

            _service = new FileTransferEventService(_db, NullLogger<FileTransferEventService>.Instance);
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public async Task SameHashOnSameDevice_IsAcceptedWithByteHashMatch()
        {
            var response = await IngestAsync(DeviceA, OrgA, Observation(recipient: "Consultant@Example.com"));

            Assert.True(response.Success);
            Assert.Equal(1, response.Data!.Accepted);

            var stored = await _db.FileTransferEvents.AsNoTracking().SingleAsync();
            Assert.Equal("BYTE_HASH_MATCH", stored.FileEvidenceType);
            Assert.Equal("HIGH", stored.FileEvidenceLevel);
            Assert.Equal("HIGH", stored.RecipientEvidenceLevel);
            Assert.Equal("HIGH", stored.OverallEvidenceLevel);
            Assert.Equal("Secret", stored.ClassificationTierAtEvent);
            Assert.NotNull(stored.FileId);
            Assert.Equal(1, stored.CorrelationRuleVersion);
        }

        [Fact]
        public async Task OutlookChannel_IsAccepted_AndUnknownChannelIsRejected()
        {
            var outlook = Observation();
            outlook.Channel = "OUTLOOK";
            var unknown = Observation(recipient: "b@example.com");
            unknown.Channel = "SOMEWHERE";

            var response = await IngestAsync(DeviceA, OrgA, outlook, unknown);

            Assert.Equal(1, response.Data!.Accepted);
            Assert.Equal(1, response.Data.RejectedInvalid);
            var stored = await _db.FileTransferEvents.AsNoTracking().SingleAsync();
            Assert.Equal("OUTLOOK", stored.Channel);
        }

        [Fact]
        public async Task RecipientAddress_IsStoredNormalized()
        {
            await IngestAsync(DeviceA, OrgA, Observation(recipient: "  Consultant@Example.COM "));

            var stored = await _db.FileTransferEvents.AsNoTracking().SingleAsync();
            Assert.Equal("consultant@example.com", stored.RecipientValue);
        }

        [Fact]
        public async Task ReplayedSend_IsCountedAsDuplicate_AndNotStoredTwice()
        {
            var batchEvent = Observation(recipient: "a@example.com");
            await IngestAsync(DeviceA, OrgA, batchEvent);

            var replay = await IngestAsync(DeviceA, OrgA, batchEvent);

            Assert.Equal(0, replay.Data!.Accepted);
            Assert.Equal(1, replay.Data.Duplicates);
            Assert.Equal(1, await _db.FileTransferEvents.CountAsync());
        }

        [Fact]
        public async Task TwoRecipientsOfOneSend_AreTwoRows()
        {
            var send = Guid.NewGuid();
            var response = await IngestAsync(DeviceA, OrgA,
                Observation(send, 0, "a@example.com"),
                Observation(send, 1, "b@example.com"));

            Assert.Equal(2, response.Data!.Accepted);
            Assert.Equal(2, await _db.FileTransferEvents.CountAsync(x => x.SendActionId == send));
        }

        [Fact]
        public async Task HashOnlyOnAnotherDevice_IsRejectedAsNoFileMatch()
        {
            // The same bytes on another device carry a different watermark, so the hash is not matched across devices.
            var response = await IngestAsync(DeviceB, OrgA, Observation(hash: Hash, name: "unknown.pdf", size: 99));

            Assert.Equal(1, response.Data!.RejectedNoFileMatch);
            Assert.Equal(0, await _db.FileTransferEvents.CountAsync());
        }

        [Fact]
        public async Task PublicFile_IsRecordedWithPublicTier()
        {
            var response = await IngestAsync(DeviceA, OrgA, Observation(hash: OtherHash, name: "[Public] notes.txt", size: 5));

            Assert.Equal(1, response.Data!.Accepted);
            var stored = await _db.FileTransferEvents.AsNoTracking().SingleAsync();
            Assert.Equal("Public", stored.ClassificationTierAtEvent);
        }

        [Fact]
        public async Task BodyIdentityNotMatchingToken_IsRefused()
        {
            var request = new FileTransferEventBatchRequestDto
            {
                TenantId = OrgB,
                DeviceId = DeviceA,
                Events = [Observation()]
            };

            var response = await _service.IngestAsync(OrgA, DeviceA, request, CancellationToken.None);

            Assert.False(response.Success);
            Assert.Equal(0, await _db.FileTransferEvents.CountAsync());
        }

        [Fact]
        public async Task EmailKindWithChatEvidence_IsRejectedAsInvalid()
        {
            var bad = Observation();
            bad.RecipientEvidenceType = "TEAMS_CHAT_NAME";

            var response = await IngestAsync(DeviceA, OrgA, bad);

            Assert.Equal(1, response.Data!.RejectedInvalid);
            Assert.Equal(0, await _db.FileTransferEvents.CountAsync());
        }

        [Fact]
        public async Task MalformedHash_IsRejectedAsInvalid()
        {
            var bad = Observation();
            bad.FileHashBytes = "not-a-hash";

            var response = await IngestAsync(DeviceA, OrgA, bad);

            Assert.Equal(1, response.Data!.RejectedInvalid);
        }

        [Fact]
        public async Task TimestampFarFromServerClock_IsRejectedAsInvalid()
        {
            var bad = Observation();
            bad.ObservedAtUtc = DateTimeOffset.UtcNow.AddYears(-3);

            var response = await IngestAsync(DeviceA, OrgA, bad);

            Assert.Equal(1, response.Data!.RejectedInvalid);
        }

        [Fact]
        public async Task ListAsync_IsScopedToTheCallersOrganization()
        {
            await IngestAsync(DeviceA, OrgA, Observation());
            _db.FileTransferEvents.Add(new FileTransferEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgB,
                SendActionId = Guid.NewGuid(),
                RecipientOrdinal = 0,
                SourceDeviceId = DeviceA,
                Channel = "GMAIL",
                RecipientKind = "EMAIL",
                RecipientValue = "x@example.com",
                RecipientEvidenceType = "EMAIL_IN_SEND_FORM",
                FileName = "x.pdf",
                ClassificationTierAtEvent = "Secret",
                FileEvidenceType = "BYTE_HASH_MATCH",
                FileEvidenceLevel = "HIGH",
                RecipientEvidenceLevel = "HIGH",
                OverallEvidenceLevel = "HIGH",
                ObservedAtUtc = Now,
                ReceivedAtUtc = Now,
                CorrelationRuleVersion = 1
            });
            await _db.SaveChangesAsync();

            var list = await _service.ListAsync(OrgA, null, null, null, null, 1, 20, CancellationToken.None);

            Assert.Equal(1, list.Data!.TotalCount);
            Assert.All(list.Data.Items, item => Assert.Equal("PC-A", item.SourceDeviceName));
        }

        private async Task<ApiResponse<FileTransferEventBatchResultDto>> IngestAsync(Guid deviceId, Guid organizationId, params FileTransferObservationDto[] events)
        {
            return await _service.IngestAsync(organizationId, deviceId, new FileTransferEventBatchRequestDto
            {
                TenantId = organizationId,
                DeviceId = deviceId,
                Events = events.ToList()
            }, CancellationToken.None);
        }

        private static FileTransferObservationDto Observation(
            Guid? send = null,
            int ordinal = 0,
            string recipient = "a@example.com",
            string? hash = Hash,
            string name = "[Secret] plan.pdf",
            long size = 10) => new()
        {
            SendActionId = send ?? Guid.NewGuid(),
            RecipientOrdinal = ordinal,
            Channel = "GMAIL",
            RecipientRole = "TO",
            RecipientKind = "EMAIL",
            RecipientValue = recipient,
            RecipientEvidenceType = "EMAIL_IN_SEND_FORM",
            FileName = name,
            FileSizeBytes = size,
            FileHashBytes = hash,
            ObservedAtUtc = Now
        };

        private static Device Device(Guid id, Guid org, string key, string machine) => new()
        {
            Id = id,
            OrganizationId = org,
            DeviceKey = key,
            MachineName = machine,
            CreatedAtUtc = Now
        };

        private static FileInventoryRecord Record(Guid deviceId, Guid org, string name, string hash, long size, string tier) => new()
        {
            FileId = Guid.NewGuid(),
            OrganizationId = org,
            DeviceId = deviceId,
            FilePath = @"C:\Docs\" + name,
            FileName = name,
            FileHash = hash,
            SizeBytes = size,
            ClassificationTier = tier,
            Provenance = "SelfCreated",
            LastScanAtUtc = Now,
            FirstSeenAtUtc = Now,
            UpdatedAtUtc = Now,
            IsDeleted = false
        };
    }
}
