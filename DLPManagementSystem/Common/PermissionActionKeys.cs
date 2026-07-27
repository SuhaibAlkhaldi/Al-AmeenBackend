namespace DLPManagementSystem.Common;

public static class PermissionActionKeys
{
    // Employee-targeted exception to the default watermark protection.
    // An effective Allow removes the overlay; absence or Deny keeps it visible.
    public const string WatermarkDisable = "watermark.disable";
}