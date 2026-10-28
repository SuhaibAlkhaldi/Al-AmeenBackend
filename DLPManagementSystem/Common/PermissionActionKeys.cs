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

    // Mirrors CompanyDlp.Contracts.ActionKeys.FileOpenAccess on the agent side - gates opening a
    // file the employee did NOT create locally themselves (received via browser download or USB
    // copy; the agent applies this automatically, there is no employee opt-in). Applies to every
    // classification tier including Public. Tier-scoped grants for this action use an EXACT tier
    // match on the agent side (see CompanyDlp.Core.PermissionEvaluator.ActionsRequiringExactTierMatch),
    // same as FileWatermarkDisable above - a "Secret" grant here does not also cover Internal/Public
    // received files. An admin's blanket "open anything received, any tier" grant is just an
    // ordinary grant with ClassificationTier left null (see PermissionGrantService.BuildGrantAsync) -
    // no separate wildcard value needed.
    public const string FileOpenAccess = "file.open-access";

    // Mirrors CompanyDlp.Contracts.ActionKeys.FilenameTagging on the agent side. An effective
    // Allow means FileInventoryScanner renames a classified file to add its tier prefix (e.g.
    // "[Secret] report.pdf") to the filename - see FilenameClassificationTagger. Replaces the old
    // local-only FileClassificationPolicy.FilenameTaggingEnabled flag, which PolicyStore's
    // PreserveLocalOnlySections discarded from every sync. Uses the default "this tier and
    // anything less sensitive" widening (not in PermissionEvaluator.ActionsRequiringExactTierMatch)
    // since this is a cosmetic/informational marker, not a strict per-tier gate like
    // FileWatermarkDisable/FileOpenAccess above - in practice this is almost always granted as a
    // blanket Organization-wide Allow with no ClassificationTier set at all.
    public const string FileFilenameTagging = "file.filename-tagging";

    // Mirrors CompanyDlp.Contracts.ActionKeys.CliSensitiveCommand on the agent side. Every event
    // reported under this key already represents a detected match - see AgentAuditEventService's
    // independent (regardless-of-Decision) alert branch for this key.
    public const string CliSensitiveCommand = "cli.sensitive-command";

    // Seeded as a "System" category action ("Housekeeping events emitted by the agent's own session
    // lifecycle") - reused here for AuditEvents DeviceStaleDetectionWorker raises itself (not reported
    // by any agent), since a device-staleness signal isn't a real grantable Allow/Deny action and
    // shouldn't get its own row in the admin's permission-management UI.
    public const string AgentSession = "agent.session";

    // Same "System" category pattern as AgentSession above - reused for every synthetic AuditEvent
    // the backend itself raises from watching FileVersionHistory (not reported by any agent), across
    // Phase 4 (immediate classification-downgrade alert, in AgentFileInventoryService) and Phase 5
    // (RapidFilePropagation, its own background worker) of the "File Tracking" plan. Neither is a
    // real grantable Allow/Deny action.
    public const string FileInventoryMonitor = "file.inventory-monitor";

    // Fixed, pre-seeded bypass action (NOT one of the dynamic "extension.<id>" keys - see
    // PermissionGrantService's extension get-or-create logic for those). An effective Allow for this
    // key on a given Employee/Device/Organization means BrowserPolicyManager (agent side) skips
    // ExtensionInstallBlocklist="*" for that subject entirely, regardless of the raw
    // BrowserPolicy.BlockUnapprovedExtensions flag - the same "unrestricted" concept as any other
    // fixed grantable action, just scoped to browser extensions.
    public const string ExtensionUnrestricted = "extension.unrestricted";

    // Prefix shared by every dynamic "approve this specific extension ID" action key. Note
    // ExtensionUnrestricted above also starts with this same prefix despite being a different, fixed
    // concept - CompanyDlp.Core.PermissionEvaluator.EvaluateAllowedKeysByPrefix (agent side) must
    // explicitly exclude it when enumerating approved extension IDs.
    public const string ExtensionActionKeyPrefix = "extension.";
}