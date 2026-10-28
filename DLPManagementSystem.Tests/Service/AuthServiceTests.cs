using DLPManagementSystem.DTO.Auth;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the login/change-password half of the "must change password" security fix: a login
    // response must surface MustChangePassword so the frontend can force a redirect, and a
    // successful change must clear it - see PROMPT context "Employee password security fix". Also
    // covers IsPlatformOwner (added so the frontend can hide/guard the Demo Requests nav item/route
    // for every org except the platform owner) - see PlatformOwnerPolicy.IsOwnerOrganizationCode,
    // which this flag shares with PlatformOwnerAuthorizationHandler.
    public class AuthServiceTests
    {
        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Production;
            public string ApplicationName { get; set; } = "AuthServiceTests";
            public string ContentRootPath { get; set; } = string.Empty;
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }

        private static IConfiguration Configuration() => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "unit-test-signing-key-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience"
            })
            .Build();

        private static (DLPSystemContext Db, AuthService Service, PasswordService Passwords) CreateService(
            string environmentName = "Production")
        {
            var db = TestDbContextFactory.Create();
            var passwordService = new PasswordService(new PasswordHasher<User>());
            var service = new AuthService(
                db, Configuration(), passwordService, new FakeAdminAuditLogService(),
                new FakeHostEnvironment { EnvironmentName = environmentName });
            return (db, service, passwordService);
        }

        private static User AddUser(
            DLPSystemContext db, PasswordService passwords, string password, bool mustChangePassword,
            Guid? organizationId = null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId ?? TestDbContextFactory.OrganizationId,
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

        private static Organization AddOrganization(DLPSystemContext db, string code)
        {
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = code,
                Code = code,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Organizations.Add(organization);
            db.SaveChanges();
            return organization;
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

        [Fact]
        public async Task LoginAsync_OwnerOrganizationUser_IsPlatformOwnerTrue()
        {
            var (db, service, passwords) = CreateService();
            var owner = AddOrganization(db, "UII");
            var user = AddUser(db, passwords, "CorrectHorse1!", mustChangePassword: false, organizationId: owner.Id);

            var result = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "CorrectHorse1!" });

            Assert.True(result.Success);
            Assert.True(result.Data!.User.IsPlatformOwner);
        }

        [Fact]
        public async Task LoginAsync_OtherOrganizationUser_IsPlatformOwnerFalse()
        {
            var (db, service, passwords) = CreateService();
            AddOrganization(db, "UII");
            // TestDbContextFactory's default seeded org (Code "TEST-ORG") stands in for "any other tenant".
            var user = AddUser(db, passwords, "CorrectHorse1!", mustChangePassword: false);

            var result = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "CorrectHorse1!" });

            Assert.True(result.Success);
            Assert.False(result.Data!.User.IsPlatformOwner);
        }

        [Fact]
        public async Task GetCurrentUserAsync_DevOrganizationUser_IsPlatformOwnerTrue_OnlyInDevelopment()
        {
            var (devDb, devService, devPasswords) = CreateService(Environments.Development);
            var devOrg = AddOrganization(devDb, "DEV");
            var devUser = AddUser(devDb, devPasswords, "CorrectHorse1!", mustChangePassword: false, organizationId: devOrg.Id);

            var devResult = await devService.GetCurrentUserAsync(devUser.Id);

            Assert.True(devResult.Success);
            Assert.True(devResult.Data!.IsPlatformOwner);

            var (prodDb, prodService, prodPasswords) = CreateService(Environments.Production);
            var prodOrg = AddOrganization(prodDb, "DEV");
            var prodUser = AddUser(prodDb, prodPasswords, "CorrectHorse1!", mustChangePassword: false, organizationId: prodOrg.Id);

            var prodResult = await prodService.GetCurrentUserAsync(prodUser.Id);

            Assert.True(prodResult.Success);
            Assert.False(prodResult.Data!.IsPlatformOwner);
        }
    }
}
