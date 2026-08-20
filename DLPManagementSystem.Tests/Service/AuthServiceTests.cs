using DLPManagementSystem.DTO.Auth;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the login/change-password half of the "must change password" security fix: a login
    // response must surface MustChangePassword so the frontend can force a redirect, and a
    // successful change must clear it - see PROMPT context "Employee password security fix".
    public class AuthServiceTests
    {
        private static IConfiguration Configuration() => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "unit-test-signing-key-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        private static (DLPSystemContext Db, AuthService Service, PasswordService Passwords) CreateService()
        {
            var db = TestDbContextFactory.Create();
            var passwordService = new PasswordService(new PasswordHasher<User>());
            var service = new AuthService(db, Configuration(), passwordService, new FakeAdminAuditLogService());
            return (db, service, passwordService);
        }

        private static User AddUser(DLPSystemContext db, PasswordService passwords, string password, bool mustChangePassword)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                FullName = "Test User",
                Email = $"{Guid.NewGuid():N}@example.local",
                PasswordHash = string.Empty,
                RoleId = 5, // Employee, per TestDbContextFactory's seeded role ids
                UserTypeId = 2,
                StatusId = 1,
                IsEmailVerified = true,
                MustChangePassword = mustChangePassword,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            user.PasswordHash = passwords.HashPassword(user, password);
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        [Fact]
        public async Task LoginAsync_UserWithMustChangePasswordTrue_ReflectsFlagInResponse()
        {
            var (db, service, passwords) = CreateService();
            var user = AddUser(db, passwords, "CorrectHorse1!", mustChangePassword: true);

            var result = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "CorrectHorse1!" });

            Assert.True(result.Success);
            Assert.True(result.Data!.User.MustChangePassword);
        }

        [Fact]
        public async Task LoginAsync_UserWithMustChangePasswordFalse_ReflectsFlagInResponse()
        {
            var (db, service, passwords) = CreateService();
            var user = AddUser(db, passwords, "CorrectHorse1!", mustChangePassword: false);

            var result = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "CorrectHorse1!" });

            Assert.True(result.Success);
            Assert.False(result.Data!.User.MustChangePassword);
        }

        [Fact]
        public async Task ChangePasswordAsync_Success_ClearsMustChangePasswordFlag()
        {
            var (db, service, passwords) = CreateService();
            var user = AddUser(db, passwords, "CorrectHorse1!", mustChangePassword: true);

            var result = await service.ChangePasswordAsync(user.Id, new ChangePasswordDto
            {
                CurrentPassword = "CorrectHorse1!",
                NewPassword = "BrandNewPassword1!"
            });

            Assert.True(result.Success);
            var reloaded = await db.Users.SingleAsync(x => x.Id == user.Id);
            Assert.False(reloaded.MustChangePassword);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_DoesNotClearFlag()
        {
            var (db, service, passwords) = CreateService();
            var user = AddUser(db, passwords, "CorrectHorse1!", mustChangePassword: true);

            var result = await service.ChangePasswordAsync(user.Id, new ChangePasswordDto
            {
                CurrentPassword = "WrongPassword1!",
                NewPassword = "BrandNewPassword1!"
            });

            Assert.False(result.Success);
            var reloaded = await db.Users.SingleAsync(x => x.Id == user.Id);
            Assert.True(reloaded.MustChangePassword);
        }
    }
}
