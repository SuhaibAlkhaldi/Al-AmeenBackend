namespace DLPManagementSystem.Helper.FileInventory
{
    public sealed class FileInventoryCleanupOptions
    {
        // Unlike AuditRetentionOptions (off by default, requires an explicit positive RetentionDays),
        // this is part of the agreed File Inventory Report design itself - "soft-delete + hard-delete
        // after 90 days" - not an opt-in archival feature, so it defaults on.
        public int RetentionDays { get; set; } = 90;
        public int BatchSize { get; set; } = 1000;

        public bool IsEnabled => RetentionDays > 0;
    }
}
