using DLPManagementSystem.Common;
using DLPManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace DLPManagementSystem.Authorization
{
    // Grants the "PlatformOwner" policy only to callers whose organizationId claim resolves (in the
    // database, not just by trusting the claim) to the platform-owner organization - the real
    // production tenant (Code "UII"), plus the local dev seeder's org (Code "DEV") but only while
    // running in Development. Used to lock down endpoints that serve platform-wide data (e.g.
    // marketing-landing-page demo requests) that must stay invisible to every other tenant's
    // SuperAdmin, even though those endpoints otherwise require only the SuperAdmin role.
    public class PlatformOwnerAuthorizationHandler : AuthorizationHandler<PlatformOwnerRequirement>
    {
        private readonly DLPSystemContext _db;
        private readonly IHostEnvironment _environment;

        public PlatformOwnerAuthorizationHandler(DLPSystemContext db, IHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PlatformOwnerRequirement requirement)
        {
            var organizationId = context.User.GetOrganizationId();
            if (organizationId == Guid.Empty)
            {
                return;
            }

            var organizationCode = await _db.Organizations
                .AsNoTracking()
                .Where(o => o.Id == organizationId)
                .Select(o => o.Code)
                .FirstOrDefaultAsync();

            if (organizationCode is null)
            {
                return;
            }

            var isPlatformOwner = PlatformOwnerPolicy.IsOwnerOrganizationCode(organizationCode, _environment.IsDevelopment());

            if (isPlatformOwner)
            {
                context.Succeed(requirement);
            }
        }
    }
}
