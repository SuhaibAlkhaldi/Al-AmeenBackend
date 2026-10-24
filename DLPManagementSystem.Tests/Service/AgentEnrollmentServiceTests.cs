using DLPManagementSystem.DTO.AgentEnrollment;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the uninstall/reinstall re-enrollment fix: a device that was properly unenrolled (left
    // "Retired", never hard-deleted - see AgentEnrollmentService.Unenroll / DeviceService.
    // DeleteDeviceAsync) must be able to re-enroll under the SAME DeviceId (identity.json on the agent
    // is never erased by uninstall by design), while a device that still exists under any other status,
    // or a same-Id device belonging to a different organization, keeps the original rejection.
    public class AgentEnrollmentServiceTests
    {
        private static User AddCreator(DLPSystemContext db)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                FullName = "Admin User",
                Email = $"{Guid.NewGuid():N}@example.local",
                PasswordHash = "irrelevant-for-this-test",
                RoleId = 1,
                UserTypeId = 1,
                StatusId = 1,
                IsEmailVerified = true,
                MustChangePassword = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static AgentEnrollmentToken AddToken(
            DLPSystemContext db,
            Guid createdByUserId,
            string plainCode,
            Guid? organizationId = null,
            int maxUses = 0)
        {
            var token = new AgentEnrollmentToken
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId ?? TestDbContextFactory.OrganizationId,
                TokenHash = SecurityHashHelper.Sha256(plainCode),
                DisplayName = "Test Token",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
                MaxUses = maxUses,
                UsedCount = 0,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.AgentEnrollmentTokens.Add(token);
            db.SaveChanges();
            return token;
        }

        private static AgentEnrollRequestDto BuildRequest(Guid tenantId, Guid deviceId, string code) => new()
        {
            TenantId = tenantId,
            DeviceId = deviceId,
            MachineName = "TEST-MACHINE",
            AgentVersion = "1.2.3",
            EnrollmentCode = code
        };

        private static AgentEnrollmentService CreateService(DLPSystemContext db) =>
            new(db, NullLogger<AgentEnrollmentService>.Instance);

        [Fact]
        public async Task Enroll_NewDeviceId_CreatesActiveDeviceAndIssuesCredential()
        {
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            var token = AddToken(db, creator.Id, "code-1");
            var deviceId = Guid.NewGuid();
            var service = CreateService(db);

            var result = await service.Enroll(BuildRequest(token.OrganizationId, deviceId, "code-1"));

            Assert.True(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Data!.AccessToken));

            var device = await db.Devices.Include(x => x.Status).FirstAsync(x => x.Id == deviceId);
            Assert.Equal("Active", device.Status.Name);
        }

        [Fact]
        public async Task Enroll_ExistingActiveDevice_ReturnsAlreadyEnrolledFailure()
        {
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            var token = AddToken(db, creator.Id, "code-2");
            var existing = TestDbContextFactory.AddDevice(db, deviceStatusId: 2);
            var service = CreateService(db);

            var result = await service.Enroll(BuildRequest(token.OrganizationId, existing.Id, "code-2"));

            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(1)] // PendingEnrollment
        [InlineData(3)] // Disabled
        [InlineData(4)] // Lost
        public async Task Enroll_ExistingDisabledOrLostOrPendingDevice_ReturnsAlreadyEnrolledFailure(int deviceStatusId)
        {
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            var token = AddToken(db, creator.Id, "code-3");
            var existing = TestDbContextFactory.AddDevice(db, deviceStatusId: deviceStatusId);
            var service = CreateService(db);

            var result = await service.Enroll(BuildRequest(token.OrganizationId, existing.Id, "code-3"));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Enroll_ExistingRetiredDeviceSameOrganization_ReactivatesDeviceAndIssuesNewCredential()
        {
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            var token = AddToken(db, creator.Id, "code-4", maxUses: 5);
            var retiredDevice = TestDbContextFactory.AddDevice(db, deviceStatusId: 5); // Retired

            var oldCredential = new DeviceCredential
            {
                Id = Guid.NewGuid(),
                DeviceId = retiredDevice.Id,
                SecretHash = "old-secret-hash",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-30),
                RevokedAtUtc = null
            };
            db.DeviceCredentials.Add(oldCredential);
            db.SaveChanges();

            var service = CreateService(db);

            var result = await service.Enroll(BuildRequest(token.OrganizationId, retiredDevice.Id, "code-4"));

            Assert.True(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Data!.AccessToken));

            var device = await db.Devices.Include(x => x.Status).FirstAsync(x => x.Id == retiredDevice.Id);
            Assert.Equal("Active", device.Status.Name);

            var credentials = await db.DeviceCredentials.Where(x => x.DeviceId == retiredDevice.Id).ToListAsync();
            Assert.Equal(2, credentials.Count);
            Assert.NotNull(credentials.Single(x => x.Id == oldCredential.Id).RevokedAtUtc);
            Assert.Null(credentials.Single(x => x.Id != oldCredential.Id).RevokedAtUtc);

            var refreshedToken = await db.AgentEnrollmentTokens.FirstAsync(x => x.Id == token.Id);
            Assert.Equal(1, refreshedToken.UsedCount);
        }

        [Fact]
        public async Task Enroll_ExistingRetiredDeviceDifferentOrganization_ReturnsAlreadyEnrolledFailure()
        {
            var db = TestDbContextFactory.Create();
            var creator = AddCreator(db);
            var otherOrganizationId = Guid.NewGuid();
            var token = AddToken(db, creator.Id, "code-5", organizationId: otherOrganizationId);
            // Retired device exists under the DEFAULT test org, but the token (and request) belong to a
            // different organization - must not be reactivated/reassigned across tenants.
            var retiredDevice = TestDbContextFactory.AddDevice(db, deviceStatusId: 5);
            var service = CreateService(db);

            var result = await service.Enroll(BuildRequest(otherOrganizationId, retiredDevice.Id, "code-5"));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Unenroll_ActiveDevice_SetsRetiredAndRevokesActiveCredentials()
        {
            var db = TestDbContextFactory.Create();
            var device = TestDbContextFactory.AddDevice(db, deviceStatusId: 2);
            var credential = new DeviceCredential
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                SecretHash = "secret-hash",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                RevokedAtUtc = null
            };
            db.DeviceCredentials.Add(credential);
            db.SaveChanges();
            var service = CreateService(db);

            var result = await service.Unenroll(TestDbContextFactory.OrganizationId, device.Id);

            Assert.True(result.Success);
            Assert.True(result.Data!.Unenrolled);

            var refreshedDevice = await db.Devices.Include(x => x.Status).FirstAsync(x => x.Id == device.Id);
            Assert.Equal("Retired", refreshedDevice.Status.Name);

            var refreshedCredential = await db.DeviceCredentials.FirstAsync(x => x.Id == credential.Id);
            Assert.NotNull(refreshedCredential.RevokedAtUtc);
        }

        [Fact]
        public async Task Unenroll_AlreadyRetiredDevice_IsIdempotentAndSucceeds()
        {
            var db = TestDbContextFactory.Create();
            var device = TestDbContextFactory.AddDevice(db, deviceStatusId: 5);
            var service = CreateService(db);

            var result = await service.Unenroll(TestDbContextFactory.OrganizationId, device.Id);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task Unenroll_DeviceNotFound_ReturnsFailure()
        {
            var db = TestDbContextFactory.Create();
            var service = CreateService(db);

            var result = await service.Unenroll(TestDbContextFactory.OrganizationId, Guid.NewGuid());

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Unenroll_DeviceBelongsToDifferentOrganization_ReturnsFailure()
        {
            var db = TestDbContextFactory.Create();
            var device = TestDbContextFactory.AddDevice(db, deviceStatusId: 2);
            var service = CreateService(db);

            var result = await service.Unenroll(Guid.NewGuid(), device.Id);

            Assert.False(result.Success);
        }
    }
}
