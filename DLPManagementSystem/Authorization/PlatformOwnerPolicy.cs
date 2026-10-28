namespace DLPManagementSystem.Authorization
{
    // Single source of truth for the platform-owner policy name and the Organization.Code values
    // that satisfy it. Not config-driven on purpose - this identifies Al-Ameen itself (the vendor),
    // not a tenant setting, so it must not be editable via appsettings/web.config.
    public static class PlatformOwnerPolicy
    {
        public const string Name = "PlatformOwner";

        // The production platform-owner organization (Unlimited Innovation). Always allowed.
        public const string OwnerOrganizationCode = "UII";

        // DatabaseSeeder creates a local organization with this code so developers have something to
        // log in as. It must only count as the platform owner while running in Development - see
        // PlatformOwnerAuthorizationHandler - so a non-dev environment can never be satisfied by it.
        public const string DevOrganizationCode = "DEV";
    }
}
