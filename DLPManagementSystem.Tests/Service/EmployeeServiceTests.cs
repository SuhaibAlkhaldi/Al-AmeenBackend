using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Employees;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Tests.Service
{
    // Covers two things together: the "optional password" security fix (EmployeeService.CreateEmployeeAsync
    // must generate a real password when none is supplied, return it exactly once, and force a change on
    // first sign-in) and the account-device binding rule (an Employee-tab account must always have a
    // device from the moment it exists) - see PROMPT context "Employee password security fix" and
    // "Account-device binding rule".
    public class EmployeeServiceTests
    {
        private static (DLPSystemContext Db, EmployeeService Service) CreateService()
        {
            var db = TestDbContextFactory.Create();
            var passwordService = new PasswordService(new PasswordHasher<User>());
            var deviceService = new DeviceService(db, new FakePolicyVersionService(), new FakeAdminAuditLogService());
            var service = new EmployeeService(db, new FakeAdminAuditLogService(), passwordService, deviceService);
            return (db, service);
        }

        private static CreateEmployeeDto ValidRequest(Guid? deviceId, string? password = null) => new()
        {
            EmployeeNumber = $"EMP-{Guid.NewGuid():N}"[..12],
            DisplayName = "Jane Doe",
            Email = $"{Guid.NewGuid():N}@example.local",
            Password = password,
            DeviceId = deviceId
        };

        [Fact]
        public async Task CreateEmployeeAsync_WithoutDevice_IsRejected()
        {
            var (_, service) = CreateService();

            var result = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest(deviceId: null));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithInactiveDevice_IsRejected()
        {
            var (db, service) = CreateService();
            var pendingDevice = TestDbContextFactory.AddDevice(db, deviceStatusId: 1); // "PendingEnrollment", not Active

            var result = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest(pendingDevice.Id));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithActiveDevice_SucceedsAndLinksTheDevice()
        {
            var (db, service) = CreateService();
            var device = TestDbContextFactory.AddDevice(db);

            var result = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest(device.Id));

            Assert.True(result.Success);
            var assignment = await db.DeviceUserAssignments.SingleAsync(x => x.DeviceId == device.Id && x.UnassignedAtUtc == null);
            Assert.Equal(result.Data!.Id, assignment.EmployeeId);
            Assert.True(assignment.IsPrimary);
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithoutPassword_GeneratesAndReturnsAUsablePasswordOnce()
        {
            var (db, service) = CreateService();
            var device = TestDbContextFactory.AddDevice(db);

            var result = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest(device.Id));

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            var generated = result.Data!.GeneratedPassword;
            Assert.False(string.IsNullOrWhiteSpace(generated));
            Assert.True(generated!.Length >= PasswordPolicy.MinLength);

            // The generated password must actually work - not another unusable placeholder hash.
            var linkedUser = await db.Users.SingleAsync(x => x.Id == result.Data.UserId);
            var passwordService = new PasswordService(new PasswordHasher<User>());
            Assert.True(passwordService.VerifyAndUpgrade(linkedUser, generated));

            Assert.True(linkedUser.MustChangePassword);
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithPassword_DoesNotEchoItBackAndStillForcesChange()
        {
            var (db, service) = CreateService();
            var device = TestDbContextFactory.AddDevice(db);

            var result = await service.CreateEmployeeAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest(device.Id, "AdminChosenPassword1!"));

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            // Never echo an admin-supplied password back over the wire - only a generated one is
            // ever returned, since the admin already knows the one they typed themselves.
            Assert.Null(result.Data!.GeneratedPassword);

            var linkedUser = await db.Users.SingleAsync(x => x.Id == result.Data.UserId);
            var passwordService = new PasswordService(new PasswordHasher<User>());
            Assert.True(passwordService.VerifyAndUpgrade(linkedUser, "AdminChosenPassword1!"));
            Assert.True(linkedUser.MustChangePassword);
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithTooShortPassword_FailsWithPasswordPolicyMessage()
        {
            var (db, service) = CreateService();
            var device = TestDbContextFactory.AddDevice(db);

            var result = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest(device.Id, "short"));

            Assert.False(result.Success);
            Assert.Equal(PasswordPolicy.MessageEn, result.MessageEn);
        }

        [Fact]
        public async Task CreateEmployeeAsync_GeneratedPasswordsAreNotAllIdentical()
        {
            var (db, service) = CreateService();
            var deviceOne = TestDbContextFactory.AddDevice(db);
            var deviceTwo = TestDbContextFactory.AddDevice(db);

            var first = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest(deviceOne.Id));
            var second = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest(deviceTwo.Id));

            Assert.NotEqual(first.Data!.GeneratedPassword, second.Data!.GeneratedPassword);
        }
    }
}
