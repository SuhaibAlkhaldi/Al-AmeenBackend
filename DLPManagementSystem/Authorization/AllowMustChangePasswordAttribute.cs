using System;

namespace DLPManagementSystem.Authorization
{
    // Marks an action (or a whole controller) as reachable even while the caller's account still has
    // MustChangePassword=true - see MustChangePasswordFilter, which otherwise rejects every
    // authenticated request from such an account with 403. Only the endpoints a caller in that state
    // still needs (setting a new password, reading their own basic profile) should carry this.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class AllowMustChangePasswordAttribute : Attribute
    {
    }
}
