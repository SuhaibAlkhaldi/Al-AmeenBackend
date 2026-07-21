using System;

namespace DLPManagementSystem.Common
{
    public static class PermissionGrantRuntimeStatus
    {
        public static string Compute(DateTimeOffset? revokedAtUtc, DateTimeOffset? expiresAtUtc, DateTimeOffset startsAtUtc, DateTimeOffset nowUtc)
        {
            if (revokedAtUtc != null)
            {
                return "Revoked";
            }

            if (expiresAtUtc != null && expiresAtUtc <= nowUtc)
            {
                return "Expired";
            }

            if (startsAtUtc > nowUtc)
            {
                return "Pending";
            }

            return "Active";
        }
    }
}
