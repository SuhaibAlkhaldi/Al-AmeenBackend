namespace DLPManagementSystem.Common;

public static class PermissionActionKeys
{
    // Employee-targeted exception to the default watermark protection.
    // An effective Allow removes the overlay; absence or Deny keeps it visible.
    public const string WatermarkDisable = "watermark.disable";

    // Mirrors CompanyDlp.Contracts.ActionKeys.FileDecrypt on the agent side. The only action key a
    // direct grant's ClassificationTier is meaningful for - see PermissionGrantService.CreateDirectGrantAsync.
    public const string FileDecrypt = "file.decrypt";

    // Mirrors CompanyDlp.Contracts.ActionKeys.CliSensitiveCommand on the agent side. Every event
    // reported under this key already represents a detected match - see AgentAuditEventService's
    // independent (regardless-of-Decision) alert branch for this key.
    public const string CliSensitiveCommand = "cli.sensitive-command";
}