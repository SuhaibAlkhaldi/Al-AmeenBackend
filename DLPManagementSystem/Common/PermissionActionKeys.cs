namespace DLPManagementSystem.Common;

public static class PermissionActionKeys
{
    // Employee-targeted exception to the default watermark protection.
    // An effective Allow removes the overlay; absence or Deny keeps it visible.
    public const string WatermarkDisable = "watermark.disable";

    // Mirrors CompanyDlp.Contracts.ActionKeys.FileDecrypt on the agent side. The only action key a
    // direct grant's ClassificationTier is meaningful for - see PermissionGrantService.CreateDirectGrantAsync.
    public const string FileDecrypt = "file.decrypt";
}