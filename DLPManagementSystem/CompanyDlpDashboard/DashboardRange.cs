namespace DLPManagementSystem.CompanyDlpDashboard;

public enum DashboardRangeKind
{
    SixHours,
    TwentyFourHours,
    SevenDays
}

// Shared range resolution for the split dashboard endpoints (kpis/breakdown/recent-events) - the
// controller never accepts fromUtc/toUtc from the client for these, only one of the three fixed
// presets below, so every client sees a consistent, server-computed window.
public sealed record DashboardRange(DashboardRangeKind Kind, string Key, DateTimeOffset FromUtc, DateTimeOffset ToUtc)
{
    public const string DefaultKey = "24h";

    // Floored to the start of the current minute so the window (and therefore the cache key) stays
    // stable across the many near-simultaneous requests a single dashboard page load fires, instead
    // of drifting by a few milliseconds/seconds between them and missing the cache every time.
    public static bool TryParse(string? rawRange, DateTimeOffset nowUtc, out DashboardRange range, out string? error)
    {
        var key = string.IsNullOrWhiteSpace(rawRange) ? DefaultKey : rawRange.Trim().ToLowerInvariant();

        DashboardRangeKind kind;
        TimeSpan duration;
        switch (key)
        {
            case "6h":
                kind = DashboardRangeKind.SixHours;
                duration = TimeSpan.FromHours(6);
                break;
            case "24h":
                kind = DashboardRangeKind.TwentyFourHours;
                duration = TimeSpan.FromHours(24);
                break;
            case "7d":
                kind = DashboardRangeKind.SevenDays;
                duration = TimeSpan.FromDays(7);
                break;
            default:
                range = null!;
                error = $"Invalid range '{rawRange}'. Allowed values: 6h, 24h, 7d.";
                return false;
        }

        var to = FloorToMinute(nowUtc);
        var from = to - duration;

        range = new DashboardRange(kind, key, from, to);
        error = null;
        return true;
    }

    private static DateTimeOffset FloorToMinute(DateTimeOffset value)
    {
        var utc = value.UtcDateTime;
        var floored = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, DateTimeKind.Utc);
        return new DateTimeOffset(floored, TimeSpan.Zero);
    }
}
