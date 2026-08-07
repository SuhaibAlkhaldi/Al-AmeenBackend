namespace DLPManagementSystem.Helper.DeviceMonitoring
{
    public sealed class DeviceStaleDetectionOptions
    {
        // How often the worker scans for stale devices. Deliberately much shorter than
        // StaleThresholdMinutes (mirrors PermissionGrantTransitionWorker's 15s poll against
        // minute-granularity grant expiry) so a device crossing the threshold is caught close to when
        // it actually happens, not up to one whole threshold-window late.
        public int PollIntervalMinutes { get; set; } = 5;

        // How long a device can go without a heartbeat before it's considered Stale. No default
        // baked in as "correct" - this depends on the real agent heartbeat cadence and the customer's
        // network reliability (VPN-only laptops, roaming users, etc.), and is exactly the kind of value
        // flagged back to the requester for review rather than assumed silently.
        public int StaleThresholdMinutes { get; set; } = 45;

        // No default enabled - an explicit positive StaleThresholdMinutes is required to activate the
        // worker, mirroring AuditRetentionOptions.IsEnabled's "off unless explicitly configured" pattern.
        public bool IsEnabled => StaleThresholdMinutes > 0;
    }
}
