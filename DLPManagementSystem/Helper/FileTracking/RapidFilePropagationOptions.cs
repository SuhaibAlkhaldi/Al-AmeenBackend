namespace DLPManagementSystem.Helper.FileTracking
{
    public sealed class RapidFilePropagationOptions
    {
        // How many DISTINCT employees must hold the same content inside one window to raise the rule.
        // Three is the agreed starting point (File Tracking plan, Phase 5); tune it against real data.
        public int EmployeeThreshold { get; set; } = 3;

        // Length of the sliding window. Agreed default is 30 minutes. A shorter window (e.g. 15) alerts
        // sooner but misses slower spread; the value is a deliberate product choice, not a technical one.
        public int WindowMinutes { get; set; } = 30;

        // How often the worker scans. Independent of the window: a 15-minute poll against a 30-minute window
        // still sees every qualifying burst, because each pass re-reads a horizon longer than the window.
        public int PollIntervalMinutes { get; set; } = 15;

        // Turns the rule off entirely without removing the worker or its data.
        public bool IsEnabled { get; set; } = true;
    }
}
