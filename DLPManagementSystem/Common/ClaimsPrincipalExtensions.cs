using System.Security.Claims;

namespace DLPManagementSystem.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetOrganizationId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("organizationId");
            return Guid.TryParse(value, out var organizationId) ? organizationId : Guid.Empty;
        }

        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
        }

        public static int GetRoleId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("roleId");
            return int.TryParse(value, out var roleId) ? roleId : 0;
        }

        public static Guid GetDeviceId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("deviceId");
            return Guid.TryParse(value, out var deviceId) ? deviceId : Guid.Empty;
        }
    }
}
