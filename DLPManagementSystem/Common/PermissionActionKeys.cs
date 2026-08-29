namespace DLPManagementSystem.Common;

public static class PermissionActionKeys
{
    // Employee-targeted exception to the default watermark protection.
    // An effective Allow removes the overlay; absence or Deny keeps it visible.
    public const string WatermarkDisable = "watermark.disable";

    // Mirrors CompanyDlp.Contracts.ActionKeys.FileDecrypt on the agent side. The only action key a
    // direct grant's ClassificationTier is meaningful for - see PermissionGrantService.CreateDirectGrantAsync.
    public const string FileDecrypt = "file.decrypt";

    // Mirrors CompanyDlp.Contracts.ActionKeys.FilePrint on the agent side. Also meaningful for
    // ClassificationTier-scoped grants (e.g. "approved to print Secret-tier files"), same as
    // FileDecrypt above - see PermissionGrantService.CreateDirectGrantAsync.
    public const string FilePrint = "file.print";

    // Mirrors CompanyDlp.Contracts.ActionKeys.FileWatermarkDisable on the agent side - distinct from
    // WatermarkDisable above (which only ever governs the live on-screen overlay). This one governs
    // the tiled/repeating watermark stamped into a file's own content; the small corner info block
    // (Classification/Device) is never affected. Unlike WatermarkDisable, ClassificationTier-scoped
    // grants for this action use an EXACT tier match on the agent side (see
    // CompanyDlp.Core.PermissionEvaluator.ActionsRequiringExactTierMatch), not "this tier and
    // anything less sensitive" like FileDecrypt/FilePrint - a "Secret" grant here does not also
    // apply to that employee's Internal/Public files.
    public const string FileWatermarkDisable = "file.watermark-disable";

    // Mirrors CompanyDlp.Contracts.ActionKeys.CliSensitiveCommand on the agent side. Every event
    // reported under this key already represents a detected match - see AgentAuditEventService's
    // independent (regardless-of-Decision) alert branch for this key.
    public const string CliSensitiveCommand = "cli.sensitive-command";

    // Seeded as a "System" category action ("Housekeeping events emitted by the agent's own session
    // lifecycle") - reused here for AuditEvents DeviceStaleDetectionWorker raises itself (not reported
    // by any agent), since a device-staleness signal isn't a real grantable Allow/Deny action and
    // shouldn't get its own row in the admin's permission-management UI.
    public const string AgentSession = "agent.session";
}