namespace DLPManagementSystem.Helper.Retention
{
    public sealed class AuditRetentionOptions
    {
        public int RetentionDays { get; set; }
        public string ArchiveDirectory { get; set; } = "App_Data/audit-archive";
        public int BatchSize { get; set; } = 1000;

        // No default enabled — an explicit positive RetentionDays is required to activate the worker,
        // mirroring the Smtp:Host "off unless explicitly configured" dev-mode fallback pattern.
        public bool IsEnabled => RetentionDays > 0;
    }
}
