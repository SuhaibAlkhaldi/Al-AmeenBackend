namespace DLPManagementSystem.CompanyDlpDashboard.Rollup;

// dashboard-rollup: bound to config section "DashboardRollup". Two independent on/off switches,
// both default false (same "off unless explicitly configured" convention as
// AuditRetentionOptions/Smtp) - deliberately decoupled so the rollout plan in
// DASHBOARD_ENDPOINTS.md's "Rollup" section works: enable RefreshEnabled first (the worker starts
// populating/maintaining the tables), verify the numbers match the raw path, THEN flip UseRollup
// (the read path actually starts serving from them). Rolling back is just flipping UseRollup back
// to false - RefreshEnabled can keep running or not, either is safe.
public sealed class DashboardRollupOptions
{
    /// <summary>kpis/breakdown/weekly-trend/risk read from the rollup tables instead of the raw
    /// AuditEvents query when true. GetSummaryAsync/GetStatusAsync/GetRecentEventsAsync are never
    /// affected by this flag - they always read the raw tables.</summary>
    public bool UseRollup { get; set; }

    /// <summary>DashboardRollupRefreshWorker only runs its recompute cycle when true. Independent
    /// of UseRollup - this can (and during rollout, should) be on well before UseRollup is.</summary>
    public bool RefreshEnabled { get; set; }

    /// <summary>How often the worker's recompute cycle runs.</summary>
    public int RefreshIntervalSeconds { get; set; } = 60;

    /// <summary>How far back (from "now", floored to the hour) each cycle re-scans and
    /// re-aggregates raw AuditEvents for the two Hourly rollup kinds - wide enough to absorb an
    /// agent that was offline and only just reconnected. An event whose relevant timestamp is
    /// older than this when it's finally received will NOT be picked up automatically; use the
    /// manual backfill command for it instead.</summary>
    public int LateEventLookbackHours { get; set; } = 48;

    /// <summary>How many trailing days (from "today", floored to the UTC day) each cycle
    /// re-scans and re-aggregates raw AuditEvents for DashboardEmployeeDailyRollup. Independent of
    /// LateEventLookbackHours (a different grain/table), but must stay wide enough to fully cover
    /// it: a 48h lookback can straddle 3 distinct UTC calendar days (e.g. an event at 23:50 two
    /// days ago is "48h old" but its OccurredAtUtc floors to a day 3 UTC-midnights back), so a
    /// 2-day window would silently drop part of the last lookback day. Null (the default) derives
    /// a safe value automatically from LateEventLookbackHours - ceil(hours/24)+1 - so the two
    /// settings can never drift out of sync; set this explicitly only to override that derivation.
    /// This is NOT RiskLookbackDays (30) - that is a read-only window for /risk and never drives
    /// how far back this worker's regular (non-wide-sweep) recompute window reaches.</summary>
    public int? EmployeeRollupRecomputeDays { get; set; }

    /// <summary>Effective EmployeeRollupRecomputeDays - the explicit value if set, otherwise
    /// derived from LateEventLookbackHours so it can never fall short of it.</summary>
    public int EffectiveEmployeeRollupRecomputeDays =>
        EmployeeRollupRecomputeDays ?? (int)Math.Ceiling(Math.Max(1, LateEventLookbackHours) / 24.0) + 1;

    /// <summary>When true, once a day (at EmployeeRollupWideSweepHourUtc) the regular refresh
    /// cycle also re-aggregates EmployeeRollupWideSweepDays of DashboardEmployeeDailyRollup per
    /// organization - a daily safety net for mobile devices that were offline for longer than
    /// LateEventLookbackHours and only send their backlog once reconnected, without requiring a
    /// manual backfill call for that. Uses the same sp_getapplock as the regular cycle, so it never
    /// runs concurrently with a manual backfill (see DashboardRollupAdminController) or with
    /// another instance's cycle. Set to false to disable this safety net entirely and rely only on
    /// the regular EffectiveEmployeeRollupRecomputeDays window plus manual backfill for anything
    /// older.</summary>
    public bool EmployeeRollupWideSweepEnabled { get; set; } = true;

    /// <summary>How many trailing days the daily wide sweep re-aggregates - independent of
    /// RiskLookbackDays, but normally kept equal to it so a day's worth of very-late events can
    /// still affect /risk's scoring once the sweep catches up to them.</summary>
    public int EmployeeRollupWideSweepDays { get; set; } = 30;

    /// <summary>The UTC hour (0-23) the daily wide sweep is allowed to run in - pick a quiet hour
    /// for the organization's traffic. The sweep runs at most once per organization per UTC day,
    /// on the first regular cycle tick whose current UTC hour matches this value.</summary>
    public int EmployeeRollupWideSweepHourUtc { get; set; } = 3;

    /// <summary>/risk's own fixed lookback window, read from the rollup - must match
    /// SqlDlpDashboardQueryService's RiskWindowDays (30) for the two paths to agree; kept as its
    /// own setting rather than a shared constant so this file alone fully documents the rollup
    /// read path's behavior.</summary>
    public int RiskLookbackDays { get; set; } = 30;

    /// <summary>CommandTimeout (seconds) for the refresh cycle's own read/write SQL commands.</summary>
    public int RefreshCommandTimeoutSeconds { get; set; } = 30;

    /// <summary>CommandTimeout (seconds) for the manual backfill command's SQL commands - longer
    /// than the regular refresh cycle's because a backfill chunk (see BackfillChunkDays) can scan
    /// a much larger slice of AuditEvents than the rolling 48h/3-day windows above ever do.</summary>
    public int BackfillCommandTimeoutSeconds { get; set; } = 120;

    /// <summary>How many days of raw AuditEvents the manual backfill command aggregates per chunk
    /// (per call to its "do one more chunk" step) - keeps any single chunk's read bounded and
    /// resumable instead of one long-running scan of the entire table.</summary>
    public int BackfillChunkDays { get; set; } = 1;

    /// <summary>sp_getapplock wait timeout (milliseconds) when the refresh cycle tries to acquire
    /// the single distributed lock before doing any work - 0 means "don't wait, skip this tick if
    /// another instance already holds it" (see DashboardRollupRefreshService).</summary>
    public int DistributedLockTimeoutMilliseconds { get; set; } = 0;
}
