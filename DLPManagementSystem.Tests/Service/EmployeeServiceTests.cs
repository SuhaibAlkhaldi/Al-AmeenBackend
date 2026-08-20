using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Employees;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the security fix for the "optional password" gap: EmployeeService.CreateEmployeeAsync
    // used to leave an omitted password as a hidden, unguessable hash with no way for the employee
    // to ever sign in. It must now generate a real password, return it exactly once, and force a
    // change on first sign-in - see PROMPT context "Employee password security fix".
    public class EmployeeServiceTests
    {
        private static (DLPSystemContext Db, EmployeeService Service) CreateService()
        {
            var db = TestDbContextFactory.Create();
            var passwordService = new PasswordService(new PasswordHasher<User>());
            var service = new EmployeeService(db, new FakeAdminAuditLogService(), passwordService);
            return (db, service);
        }

        private static CreateEmployeeDto ValidRequest(string? password = null) => new()
        {
            EmployeeNumber = $"EMP-{Guid.NewGuid():N}"[..12],
            DisplayName = "Jane Doe",
            Email = $"{Guid.NewGuid():N}@example.local",
            Password = password
        };

        [Fact]
        public async Task CreateEmployeeAsync_WithoutPassword_GeneratesAndReturnsAUsablePasswordOnce()
        {
            var (db, service) = CreateService();

            var result = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest());

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

            var result = await service.CreateEmployeeAsync(
                TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest("AdminChosenPassword1!"));

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
            var (_, service) = CreateService();

            var result = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest("short"));

            Assert.False(result.Success);
            Assert.Equal(PasswordPolicy.MessageEn, result.MessageEn);
        }

        [Fact]
        public async Task CreateEmployeeAsync_GeneratedPasswordsAreNotAllIdentical()
        {
            var (_, service) = CreateService();

            var first = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest());
            var second = await service.CreateEmployeeAsync(TestDbContextFactory.OrganizationId, Guid.NewGuid(), ValidRequest());

            Assert.NotEqual(first.Data!.GeneratedPassword, second.Data!.GeneratedPassword);
        }
    }
}
