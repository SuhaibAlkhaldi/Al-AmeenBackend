using System.Security.Claims;
using DLPManagementSystem.Authorization;
using DLPManagementSystem.Models;
using DLPManagementSystem.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace DLPManagementSystem.Tests.Authorization
{
    // Covers the server-side MustChangePassword gate added after a live QA audit found that a
    // MustChangePassword=true account's JWT worked against every ordinary endpoint - the frontend's
    // redirect-to-change-password screen was the only thing stopping it, which curl/Postman simply
    // don't go through. See MustChangePasswordFilter.
    public class MustChangePasswordFilterTests
    {
        private static User AddUser(DLPSystemContext db, bool mustChangePassword)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = TestDbContextFactory.OrganizationId,
                FullName = "Test User",
                Email = $"{Guid.NewGuid():N}@example.local",
                PasswordHash = "irrelevant-for-this-test",
                RoleId = 5, // Employee, per TestDbContextFactory's seeded role ids
                UserTypeId = 2,
                StatusId = 1,
                IsEmailVerified = true,
                MustChangePassword = mustChangePassword,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        private static AuthorizationFilterContext CreateContext(Guid? userId, IList<object> endpointMetadata)
        {
            var httpContext = new DefaultHttpContext();

            if (userId.HasValue)
            {
                var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.Value.ToString()) }, "Bearer");
                httpContext.User = new ClaimsPrincipal(identity);
            }

            var actionDescriptor = new ActionDescriptor { EndpointMetadata = endpointMetadata };
            var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        [Fact]
        public async Task OnAuthorizationAsync_MustChangePasswordTrue_RejectsWith403()
        {
            var db = TestDbContextFactory.Create();
            var user = AddUser(db, mustChangePassword: true);
            var filter = new MustChangePasswordFilter(db);
            var context = CreateContext(user.Id, Array.Empty<object>());

            await filter.OnAuthorizationAsync(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task OnAuthorizationAsync_MustChangePasswordFalse_AllowsRequest()
        {
            var db = TestDbContextFactory.Create();
            var user = AddUser(db, mustChangePassword: false);
            var filter = new MustChangePasswordFilter(db);
            var context = CreateContext(user.Id, Array.Empty<object>());

            await filter.OnAuthorizationAsync(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public async Task OnAuthorizationAsync_ActionAllowsMustChangePassword_ReachesChangePasswordEndpointAnyway()
        {
            var db = TestDbContextFactory.Create();
            var user = AddUser(db, mustChangePassword: true);
            var filter = new MustChangePasswordFilter(db);
            var context = CreateContext(user.Id, new object[] { new AllowMustChangePasswordAttribute() });

            await filter.OnAuthorizationAsync(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public async Task OnAuthorizationAsync_NoSubClaim_AllowsRequest()
        {
            // Covers both unauthenticated/[AllowAnonymous] requests and DeviceBearer-authenticated
            // agent requests, neither of which carry a "sub" claim.
            var db = TestDbContextFactory.Create();
            var filter = new MustChangePasswordFilter(db);
            var context = CreateContext(userId: null, Array.Empty<object>());

            await filter.OnAuthorizationAsync(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public async Task OnAuthorizationAsync_AfterPasswordChangeInDb_TakesEffectWithoutANewToken()
        {
            // The JWT itself carries no MustChangePassword claim (see AuthService.GenerateAccessToken)
            // - the filter reads the current value from the database on every call, so the very next
            // request after AuthService.ChangePasswordAsync flips the flag must succeed even though
            // nothing about the caller's token changed.
            var db = TestDbContextFactory.Create();
            var user = AddUser(db, mustChangePassword: true);
            var filter = new MustChangePasswordFilter(db);

            var beforeChange = CreateContext(user.Id, Array.Empty<object>());
            await filter.OnAuthorizationAsync(beforeChange);
            Assert.NotNull(beforeChange.Result);

            user.MustChangePassword = false;
            db.SaveChanges();

            var afterChange = CreateContext(user.Id, Array.Empty<object>());
            await filter.OnAuthorizationAsync(afterChange);
            Assert.Null(afterChange.Result);
        }
    }
}
