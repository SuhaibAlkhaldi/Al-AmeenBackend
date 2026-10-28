using Microsoft.AspNetCore.Authorization;

namespace DLPManagementSystem.Authorization
{
    // Marker requirement for the "PlatformOwner" policy - see PlatformOwnerAuthorizationHandler for
    // the actual decision and PlatformOwnerPolicy for the allowed organization codes.
    public class PlatformOwnerRequirement : IAuthorizationRequirement
    {
    }
}
