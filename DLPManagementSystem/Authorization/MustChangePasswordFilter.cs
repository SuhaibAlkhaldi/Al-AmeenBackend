using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DLPManagementSystem.Common;
using DLPManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Authorization
{
    // Registered as a global MVC filter (see Program.cs) so every controller is covered without
    // having to remember to opt in. Rejects any authenticated request from a human account whose
    // MustChangePassword flag is still true, with 403, unless the action carries
    // [AllowMustChangePassword] - only AuthController's Me/ChangePassword actions do, since those
    // are the only two operations such an account still needs to reach.
    //
    // Scoped to human logins only: it looks for the "sub" claim the JWT bearer scheme issues (see
    // AuthService.GenerateAccessToken). DeviceBearerAuthenticationHandler's agent tokens never carry
    // a "sub" claim (only deviceId/organizationId), and unauthenticated/[AllowAnonymous] requests have
    // no identity at all - both cases fall through untouched.
    public class MustChangePasswordFilter : IAsyncAuthorizationFilter
    {
        private readonly DLPSystemContext _db;

        public MustChangePasswordFilter(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var subClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (subClaim == null || !Guid.TryParse(subClaim, out var userId))
            {
                return;
            }

            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowMustChangePasswordAttribute))
            {
                return;
            }

            // Read live from the database rather than trusting a claim baked into the JWT at
            // issuance: the access token carries no MustChangePassword claim at all, precisely so
            // that changing the password takes effect starting with the very next request instead of
            // requiring the caller to log in again for a fresh token.
            var mustChangePassword = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => (bool?)u.MustChangePassword)
                .FirstOrDefaultAsync();

            if (mustChangePassword == true)
            {
                context.Result = new ObjectResult(ApiResponse<object>.FailureResponse(
                    "You must change your password before continuing.",
                    "يجب عليك تغيير كلمة المرور قبل المتابعة"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
