using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DLPManagementSystem.Authorization;
using DLPManagementSystem.Common;
using DLPManagementSystem.Controllers;
using DLPManagementSystem.DTO.DemoRequests;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Tests.Authorization
{
    // Drives DemoRequestsController's GET list / PUT status actions through the REAL ASP.NET Core
    // authorization middleware (not just the handler in isolation - see
    // PlatformOwnerAuthorizationHandlerTests) to confirm what a caller actually gets back over HTTP:
    // a SuperAdmin from a non-owner organization must see 403, never an empty/successful 200, and the
    // owner org's SuperAdmin must still get 200. Hosted in a minimal TestServer (not the full
    // Program.cs pipeline, which needs a real SQL Server connection and JWT secret) with only
    // DemoRequestsController mapped, a fake IDemoRequestService standing in for the business logic
    // under test elsewhere, and a test authentication scheme that builds the caller's ClaimsPrincipal
    // straight from request headers.
    public class DemoRequestsControllerPlatformOwnerEndpointTests
    {
        private const string TestScheme = "Test";

        private sealed class FakeDemoRequestService : IDemoRequestService
        {
            public Task<ApiResponse<DemoRequestListItemDto>> CreateAsync(
                CreateDemoRequestDto request, string? sourceIp, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException("Not exercised by these authorization tests.");

            public Task<ApiResponse<PagedResultDto<DemoRequestListItemDto>>> GetListAsync(
                int? statusId, int page, int pageSize, CancellationToken cancellationToken = default) =>
                Task.FromResult(ApiResponse<PagedResultDto<DemoRequestListItemDto>>.SuccessResponse(
                    new PagedResultDto<DemoRequestListItemDto> { Items = new(), TotalCount = 0, Page = page, PageSize = pageSize }));

            public Task<ApiResponse<DemoRequestListItemDto>> UpdateStatusAsync(
                Guid id, UpdateDemoRequestStatusDto request, CancellationToken cancellationToken = default) =>
                Task.FromResult(ApiResponse<DemoRequestListItemDto>.SuccessResponse(
                    new DemoRequestListItemDto { Id = id, StatusId = request.StatusId }));
        }

        // Builds the caller's ClaimsPrincipal from two test-only headers instead of a real JWT -
        // RoleClaimType "role" mirrors the Jwt:RoleClaimType ASP.NET's real JwtBearer handler is
        // configured with in Program.cs, so [Authorize(Roles = "SuperAdmin")] evaluates the same way.
        private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public const string RoleHeader = "X-Test-Role";
            public const string OrgHeader = "X-Test-Org";

            public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var claims = new List<Claim>();

                if (Request.Headers.TryGetValue(RoleHeader, out var role))
                {
                    claims.Add(new Claim("role", role.ToString()));
                }

                if (Request.Headers.TryGetValue(OrgHeader, out var org))
                {
                    claims.Add(new Claim("organizationId", org.ToString()));
                }

                var identity = new ClaimsIdentity(claims, Scheme.Name, nameType: ClaimTypes.Name, roleType: "role");
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        // Restricts MVC's controller discovery to exactly DemoRequestsController so the test host
        // never has to satisfy every other controller's constructor dependencies.
        private sealed class SingleControllerFeatureProvider : ControllerFeatureProvider
        {
            protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(DemoRequestsController);
        }

        private static TestServer CreateServer(DLPSystemContext db, string environmentName)
        {
            var hostBuilder = new WebHostBuilder()
                .UseEnvironment(environmentName)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(db);
                    services.AddSingleton<IDemoRequestService>(new FakeDemoRequestService());

                    services.AddAuthentication(TestScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestScheme, null);

                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy(PlatformOwnerPolicy.Name, policy =>
                            policy.Requirements.Add(new PlatformOwnerRequirement()));
                    });
                    services.AddScoped<IAuthorizationHandler, PlatformOwnerAuthorizationHandler>();

                    services.AddControllers()
                        .AddApplicationPart(typeof(DemoRequestsController).Assembly)
                        .ConfigureApplicationPartManager(apm =>
                        {
                            foreach (var defaultProvider in apm.FeatureProviders
                                .Where(f => f is ControllerFeatureProvider).ToList())
                            {
                                apm.FeatureProviders.Remove(defaultProvider);
                            }

                            apm.FeatureProviders.Add(new SingleControllerFeatureProvider());
                        });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });

            return new TestServer(hostBuilder);
        }

        private static HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, Guid organizationId)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add(TestAuthHandler.RoleHeader, "SuperAdmin");
            request.Headers.Add(TestAuthHandler.OrgHeader, organizationId.ToString());
            return request;
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
        public async Task GetList_OwnerOrganizationSuperAdmin_Returns200()
        {
            using var db = TestDbContextFactory.Create();
            var owner = AddOrganization(db, "UII");
            using var server = CreateServer(db, Environments.Production);
            using var client = server.CreateClient();

            var response = await client.SendAsync(AuthenticatedRequest(HttpMethod.Get, "/api/v1/demo-requests", owner.Id));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetList_OtherOrganizationSuperAdmin_Returns403()
        {
            using var db = TestDbContextFactory.Create();
            AddOrganization(db, "UII");
            var otherOrg = AddOrganization(db, "NEWCO");
            using var server = CreateServer(db, Environments.Production);
            using var client = server.CreateClient();

            var response = await client.SendAsync(AuthenticatedRequest(HttpMethod.Get, "/api/v1/demo-requests", otherOrg.Id));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_OwnerOrganizationSuperAdmin_Returns200()
        {
            using var db = TestDbContextFactory.Create();
            var owner = AddOrganization(db, "UII");
            using var server = CreateServer(db, Environments.Production);
            using var client = server.CreateClient();

            var request = AuthenticatedRequest(HttpMethod.Put, $"/api/v1/demo-requests/{Guid.NewGuid()}/status", owner.Id);
            request.Content = JsonContent.Create(new UpdateDemoRequestStatusDto { StatusId = 2 });

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateStatus_OtherOrganizationSuperAdmin_Returns403()
        {
            using var db = TestDbContextFactory.Create();
            AddOrganization(db, "UII");
            var otherOrg = AddOrganization(db, "NEWCO");
            using var server = CreateServer(db, Environments.Production);
            using var client = server.CreateClient();

            var request = AuthenticatedRequest(HttpMethod.Put, $"/api/v1/demo-requests/{Guid.NewGuid()}/status", otherOrg.Id);
            request.Content = JsonContent.Create(new UpdateDemoRequestStatusDto { StatusId = 2 });

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
