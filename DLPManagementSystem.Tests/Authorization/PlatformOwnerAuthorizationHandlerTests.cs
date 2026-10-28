using System.Security.Claims;
using DLPManagementSystem.Authorization;
using DLPManagementSystem.Models;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DLPManagementSystem.Tests.Authorization
{
    // DemoRequestsController's GetList/UpdateStatus must be reachable only by the SuperAdmin of the
    // platform-owner organization (Code "UII" in production; "DEV" too, but only under
    // IHostEnvironment.IsDevelopment(), since that's the code DatabaseSeeder gives local dev orgs) -
    // not by the SuperAdmin of any other tenant. These tests exercise the handler directly against an
    // in-memory DB, independent of the controller/HTTP pipeline (covered separately by
    // DemoRequestsControllerPlatformOwnerEndpointTests).
    public class PlatformOwnerAuthorizationHandlerTests
    {
        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Production;
            public string ApplicationName { get; set; } = "PlatformOwnerAuthorizationHandlerTests";
            public string ContentRootPath { get; set; } = string.Empty;
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }

        private static ClaimsPrincipal UserWithOrganization(Guid? organizationId)
        {
            var claims = organizationId.HasValue
                ? new[] { new Claim("organizationId", organizationId.Value.ToString()) }
                : Array.Empty<Claim>();

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        private static async Task<bool> SucceedsAsync(DLPSystemContext db, Guid? organizationId, string environmentName)
        {
            var handler = new PlatformOwnerAuthorizationHandler(db, new FakeHostEnvironment { EnvironmentName = environmentName });
            var requirement = new PlatformOwnerRequirement();
            var context = new AuthorizationHandlerContext(
                new[] { requirement }, UserWithOrganization(organizationId), resource: null);

            await handler.HandleAsync(context);

            return context.HasSucceeded;
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
        public async Task Handler_Succeeds_ForOwnerOrganization_UII()
        {
            using var db = TestDbContextFactory.Create();
            var owner = AddOrganization(db, "UII");

            var succeeded = await SucceedsAsync(db, owner.Id, Environments.Production);

            Assert.True(succeeded);
        }

        [Fact]
        public async Task Handler_Fails_ForADifferentOrganization()
        {
            using var db = TestDbContextFactory.Create();
            AddOrganization(db, "UII");
            var otherOrg = AddOrganization(db, "NEWCO");

            var succeeded = await SucceedsAsync(db, otherOrg.Id, Environments.Production);

            Assert.False(succeeded);
        }

        [Fact]
        public async Task Handler_Fails_ForUnknownOrganizationId()
        {
            using var db = TestDbContextFactory.Create();
            AddOrganization(db, "UII");

            var succeeded = await SucceedsAsync(db, Guid.NewGuid(), Environments.Production);

            Assert.False(succeeded);
        }

        [Fact]
        public async Task Handler_Fails_WhenOrganizationClaimIsMissing()
        {
            using var db = TestDbContextFactory.Create();
            AddOrganization(db, "UII");

            var succeeded = await SucceedsAsync(db, organizationId: null, Environments.Production);

            Assert.False(succeeded);
        }

        [Fact]
        public async Task Handler_Succeeds_ForDevOrganization_OnlyInDevelopment()
        {
            using var db = TestDbContextFactory.Create();
            var devOrg = AddOrganization(db, "DEV");

            var succeeded = await SucceedsAsync(db, devOrg.Id, Environments.Development);

            Assert.True(succeeded);
        }

        [Fact]
        public async Task Handler_Fails_ForDevOrganization_OutsideDevelopment()
        {
            using var db = TestDbContextFactory.Create();
            var devOrg = AddOrganization(db, "DEV");

            var succeededInProduction = await SucceedsAsync(db, devOrg.Id, Environments.Production);
            var succeededInStaging = await SucceedsAsync(db, devOrg.Id, Environments.Staging);

            Assert.False(succeededInProduction);
            Assert.False(succeededInStaging);
        }
    }
}
