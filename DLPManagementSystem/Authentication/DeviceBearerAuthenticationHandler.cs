using System.Security.Claims;
using System.Text.Encodings.Web;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Authentication
{
    public static class DeviceBearerDefaults
    {
        public const string SchemeName = "DeviceBearer";
    }

    public class DeviceBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly DLPSystemContext _db;

        public DeviceBearerAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            DLPSystemContext db)
            : base(options, logger, encoder)
        {
            _db = db;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
            {
                return AuthenticateResult.NoResult();
            }

            var authorizationHeader = authorizationHeaderValues.ToString();

            if (string.IsNullOrWhiteSpace(authorizationHeader) ||
                !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.NoResult();
            }

            var token = authorizationHeader["Bearer ".Length..].Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                return AuthenticateResult.Fail("Missing device bearer token.");
            }

            var tokenHash = SecurityHashHelper.Sha256(token);
            var nowUtc = DateTimeOffset.UtcNow;

            var credential = await _db.DeviceCredentials
                .Include(x => x.Device).ThenInclude(x => x.Status)
                .FirstOrDefaultAsync(x =>
                    x.SecretHash == tokenHash &&
                    x.RevokedAtUtc == null &&
                    (x.RotationDueAtUtc == null || x.RotationDueAtUtc > nowUtc));

            if (credential == null)
            {
                return AuthenticateResult.Fail("Invalid or expired device token.");
            }

            if (!string.Equals(credential.Device.Status.Name, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("Device is not active.");
            }

            credential.LastUsedAtUtc = nowUtc;
            await _db.SaveChangesAsync();

            var claims = new[]
            {
                new Claim("deviceId", credential.DeviceId.ToString()),
                new Claim("organizationId", credential.Device.OrganizationId.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
