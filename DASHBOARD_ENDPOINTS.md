# Dashboard endpoints

`GET /api/v1/dashboard/summary` is the original, single heavy endpoint. It still works exactly as
before and is not going away - any client can keep using it. The six endpoints below
(dashboard-split-endpoints) split that same data into independent pieces so a slow one never holds
up a fast one.

All seven endpoints:
- Require the same auth as today: `[Authorize]`, any signed-in user (no extra role gate).
- Are scoped to the caller's own organization automatically (from the `organizationId` claim on
  the access token) - you never pass an organization id yourself.
- Return plain JSON (no `{ success, data }` wrapper).

## The `range` parameter

Five of the six split endpoints take a `range` query parameter:

| Value | Meaning |
|---|---|
| `6h` | last 6 hours |
| `24h` | last 24 hours (**default** if `range` is omitted) |
| `7d` | last 7 days |

Anything else (`1h`, `week`, `30d`, ...) returns **400 Bad Request**:
```json
{ "message": "Invalid range '1h'. Allowed values: 6h, 24h, 7d." }
```

The time window is always computed on the server - `toUtc` is "now", floored to the start of the
current minute; `fromUtc` is `toUtc` minus the range's duration. You cannot pass your own
`fromUtc`/`toUtc` to these endpoints (that's still only possible on `/summary`).

`/status`, `/risk` and `/weekly-trend` don't take `range` at all - see each one below for why.

## Caching

Every split endpoint is cached server-side, per organization, for a short time - so hitting
refresh or re-opening the dashboard right after loading it doesn't re-run the underlying query:

| Endpoint | Cache duration |
|---|---|
| `/kpis` | 30s |
| `/breakdown` | 30s |
| `/recent-events` | 30s |
| `/status` | 15s |
| `/risk` | 5 min |
| `/weekly-trend` | 5 min |

Caching is always per-organization (and, where relevant, per `range`/`limit`) - two organizations,
or the same organization with two different `range` values, never share a cached result.

---

## `GET /api/v1/dashboard/kpis?range=24h`

Headline totals for the window, the equivalent totals for the immediately-preceding window of the
same length (for a %-change indicator), and a time-bucketed series for a sparkline.

**Query**: `range` (optional, default `24h`).

```json
{
  "range": "24h",
  "fromUtc": "2026-10-12T10:00:00+00:00",
  "toUtc": "2026-10-13T10:00:00+00:00",
  "totals": {
    "totalEvents": 120,
    "allowedEvents": 90,
    "blockedEvents": 20,
    "auditOnlyEvents": 8,
    "errorEvents": 2
  },
  "previousTotals": { "totalEvents": 95, "allowedEvents": 70, "blockedEvents": 18, "auditOnlyEvents": 5, "errorEvents": 2 },
  "series": [
    { "bucketStartUtc": "2026-10-12T10:00:00+00:00", "total": 5, "blocked": 1, "allowed": 4, "auditOnly": 0, "errors": 0 }
  ]
}
```

Notes:
- `totals`/`previousTotals` deliberately **omit** `uniqueActions`, `lastReceivedAtUtc` and
  `policyVersion` from the `/summary` shape - unused by any current client, and each one is extra
  aggregation work this endpoint skips.
- `series` has one point per hour (`6h`/`24h` ranges) or per day (`7d`), zero-filled for buckets
  with no events - always a complete, contiguous array, never missing a bucket.

## `GET /api/v1/dashboard/status`

A point-in-time snapshot - no `range`, this is "right now", not a historical window.

```json
{
  "permissionRequestCounts": { "pending": 3, "approved": 40, "rejected": 5 },
  "endpointAgents": { "enrolledDeviceCount": 50, "heartbeatFreshDeviceCount": 47, "agentHeartbeatPct": 94.0 }
}
```

## `GET /api/v1/dashboard/recent-events?range=24h&limit=10`

The most recent audit events in the window, newest first.

**Query**: `range` (optional, default `24h`); `limit` (optional, default **10**, clamped to **1-20**
server-side - you don't get a 400 for an out-of-range limit, it's just capped).

```json
[
  {
    "actionKey": "usb.storage",
    "eventType": "Permission Evaluated",
    "username": "jdoe",
    "userSid": "S-1-5-21-...",
    "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "deviceName": "LAPTOP-123",
    "details": "File: report.xlsx | Process: explorer.exe",
    "decision": "Block",
    "reasonCode": "PolicyBlocked",
    "policyVersion": 7,
    "occurredAtUtc": "2026-10-13T09:58:12+00:00",
    "receivedAtUtc": "2026-10-13T09:58:14+00:00"
  }
]
```

`deviceName` is the device's current `MachineName`, resolved by joining on `deviceId`; it is `null`
if the device no longer exists.

## `GET /api/v1/dashboard/breakdown?range=24h`

Action/reason/channel breakdowns for the window.

**Query**: `range` (optional, default `24h`).

```json
{
  "actionBreakdown": [
    { "actionKey": "usb.storage", "total": 40, "allowed": 30, "blocked": 8, "errors": 2, "lastReceivedAtUtc": "..." }
  ],
  "reasonBreakdown": [
    { "reasonCode": "PolicyBlocked", "total": 8 }
  ],
  "channelBreakdown": [
    { "category": "RemovableStorage", "blocked": 5 }
  ]
}
```

`actionBreakdown` and `reasonBreakdown` are each capped at the **top 8** (by count); `channelBreakdown`
is not capped (there are only a handful of channel categories).

## `GET /api/v1/dashboard/risk`

Top risky users and per-department risk. **No `range`** - this is a fixed rolling **30-day**
window, same as `/summary`'s risk section, independent of whatever range the rest of the dashboard
is showing.

```json
{
  "topRiskyUsers": [
    {
      "employeeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "displayName": "Jane Doe",
      "departmentName": "Finance",
      "riskScore": 82,
      "riskLevel": "High",
      "topFactors": [ { "category": "RemovableStorage", "count": 6 } ]
    }
  ],
  "riskByDepartment": [ { "departmentName": "Finance", "riskScore": 55 } ],
  "highRiskUserCount": 3
}
```

`riskScore` is 0-100, relative to the highest-scoring employee in the org over the last 30 days;
`riskLevel` is `High` (>=70), `Medium` (>=40) or `Low` - same definition as `/summary` uses today.

## `GET /api/v1/dashboard/weekly-trend`

Block/violation counts per 7-day bucket over a fixed rolling **28-day** window, oldest bucket
first. **No `range`**.

```json
{
  "weeklyTrend": [
    { "weekStartUtc": "2026-09-15T10:23:00+00:00", "blockedCount": 4, "violationCount": 9 },
    { "weekStartUtc": "2026-09-22T10:23:00+00:00", "blockedCount": 2, "violationCount": 5 },
    { "weekStartUtc": "2026-09-29T10:23:00+00:00", "blockedCount": 6, "violationCount": 11 },
    { "weekStartUtc": "2026-10-06T10:23:00+00:00", "blockedCount": 3, "violationCount": 7 }
  ]
}
```

`blockedCount` counts `Block` decisions only; `violationCount` counts `Block` + `AuditOnly`/`Audit`
combined (audit-only events are still policy-flagged, just not prevented).

---

## Migration note for clients

Nothing about `/summary` changed - if you're already using it, there's no rush. When you're ready
to move to the split endpoints, each one maps to a slice of the `/summary` response:

| `/summary` field | Split endpoint |
|---|---|
| `totals`, `previousTotals`, `series` (trimmed, see above) | `/kpis` |
| `permissionRequestCounts`, endpoint-agent fields | `/status` |
| `recentEvents` (now with `deviceName`) | `/recent-events` |
| `actionBreakdown`, `reasonBreakdown`, `channelBreakdown` | `/breakdown` |
| `topRiskyUsers`, `riskByDepartment`, `highRiskUserCount` | `/risk` |
| `weeklyTrend` | `/weekly-trend` |
| `policyVersion` | not exposed on any split endpoint (unused by current clients) |

## Rollup (dashboard-rollup)

`/kpis`, `/breakdown`, `/weekly-trend` and `/risk` can optionally read from two small pre-aggregated
tables instead of scanning `dbo.AuditEvents` directly, so their latency stays roughly constant as
that table grows into the tens of millions of rows. `/summary`, `/status` and `/recent-events` are
never affected by this - they always read the raw table.

**Tables** (`DLPManagementSystem/Models/DashboardHourlyRollup.cs` /
`DashboardEmployeeDailyRollup.cs` / `DashboardRollupWatermark.cs`):
- `DashboardHourlyRollup` - one row per (org, `TimeBasis`, hour, decision, action, reason). `TimeBasis`
  is `Received` or `Occurred`, kept separate and never unified - `/kpis`/`/breakdown` read the
  `Received` rows (matching the raw path's own `ReceivedAtUtc` filter); `/weekly-trend` reads the
  `Occurred` rows. Unifying the two would change either endpoint's visible numbers.
- `DashboardEmployeeDailyRollup` - one row per (org, employee, day, action, decision), bucketed from
  `OccurredAtUtc`. Feeds `/risk`.
- `DashboardRollupWatermark` - per (org, rollup kind), how far that rollup has actually been
  computed (`LastComputedThroughUtc`, exclusive). Read by the retention worker's purge guard (below)
  and by the manual backfill command, to resume where a previous run left off.

**Window alignment and drift.** The rollup's own grain is the hour (`DashboardHourlyRollup`) or the
UTC day (`DashboardEmployeeDailyRollup`). Every window the rollup read path computes is floored to
that grain before querying, instead of using the raw path's continuous-to-the-minute/second window.
This is a deliberate, bounded difference from the raw numbers, not a bug:
- `/kpis`, `/breakdown`: drift ≤ 1 hour (the rollup's `to` is "now" floored to the hour, vs the raw
  path's `to` floored to the minute).
- `/weekly-trend`: drift ≤ 1 hour, same reason (its 28-day window is anchored off the same
  hour-floored "now").
- `/risk`: drift ≤ 1 day (its 30-day window is anchored off "now" floored to the UTC day - "today"
  is excluded from the rollup-backed window entirely until the day rolls over).

**Flags** (`DashboardRollup` config section, both default `false`):
- `RefreshEnabled` - `DashboardRollupRefreshWorker` only recomputes the rollup tables when `true`.
  Safe to turn on well before `UseRollup` - it only ever writes to the two rollup tables.
- `UseRollup` - the four endpoints above only read from the rollup tables when `true`. Flipping this
  back to `false` at any time fully reverts to the raw path; `RefreshEnabled` can keep running or not
  either way.

**Refresh mechanism.** `DashboardRollupRefreshWorker` is a `BackgroundService` (not an indexed view)
that, every `RefreshIntervalSeconds` (default 60s), calls `DashboardRollupRefreshService
.RefreshRecentWindowAsync`:
- Takes a session-scoped `sp_getapplock` (resource `DashboardRollupRefresh`) before doing any work,
  so only one app instance ever recomputes at a time - if another instance already holds it, the
  tick is skipped (not an error; expected whenever more than one instance is running, e.g. during a
  pool recycle).
- Re-aggregates only a trailing rolling window per rollup kind: `LateEventLookbackHours` (default
  48h, floored to the hour) for both `DashboardHourlyRollup` bases, and
  `EffectiveEmployeeRollupRecomputeDays` for `DashboardEmployeeDailyRollup`. The latter is
  `EmployeeRollupRecomputeDays` if set, otherwise derived as `ceil(LateEventLookbackHours/24)+1`
  (48h -> 3 days) so it can never fall short of the hourly lookback - a 48h-old event can land on a
  UTC calendar date up to 2 days before today, and the derivation's "+1" keeps a one-day margin
  against that boundary (clock drift, a tick landing right at a day rollover) rather than sitting
  exactly on it.
- **An event whose relevant timestamp is older than that lookback when it's finally received is NOT
  picked up automatically by the regular per-tick window.** This matters because `ReceivedAtUtc` is
  always "now" (set by `AgentAuditEventService` at ingestion) while `OccurredAtUtc` is agent-supplied
  and can be old (an agent that reconnects after being offline). Two ways this is covered beyond the
  regular window:
  - The manual backfill command (below), for an operator-triggered catch-up of any age.
  - The **daily wide sweep** (`EmployeeRollupWideSweepEnabled`, default `true`): once per UTC day,
    at `EmployeeRollupWideSweepHourUtc` (default `3`, pick a quiet hour for your traffic), the same
    tick that would otherwise do the regular narrow EmployeeDaily window also re-aggregates
    `EmployeeRollupWideSweepDays` (default `30`, matching `RiskLookbackDays`) instead, automatically
    catching devices that were offline for longer than `LateEventLookbackHours` and only send their
    backlog once reconnected - no manual backfill call needed for that case. It runs from inside the
    same `RefreshRecentWindowAsync` call as the regular tick, so it always holds the same
    `sp_getapplock` - it can never run concurrently with another instance's tick or with a manual
    backfill call (whichever is already holding the lock wins; the other waits or skips, same as any
    other tick). A small per-organization marker (`DashboardRollupWatermark.LastWideSweepUtc`)
    ensures it fires at most once per organization per UTC day. Set
    `EmployeeRollupWideSweepEnabled=false` to disable this safety net entirely and rely only on the
    regular window plus manual backfill for anything older.
- Visibility: the local dev database has `READ_COMMITTED_SNAPSHOT`/snapshot isolation **off**, and
  this feature does not turn it on. Instead, the (potentially slow) aggregation `SELECT` against raw
  `AuditEvents` runs with no open transaction, into memory; only the bounded delete-then-insert of
  the already-computed result set runs inside a short transaction. A dashboard reader under the
  default READ COMMITTED isolation therefore either sees the complete old rows or the complete new
  rows for a bucket - never a partially-deleted/partially-inserted one - at worst it blocks briefly
  on that short transaction's locks. Measured directly on the local database (not assumed): a
  delete+insert of ~800 rows (matching an observed real cycle's write size) takes **~100-130ms**, and
  a concurrent reader issuing the exact same query `RollupDlpDashboardQueryService` would run, while
  that transaction is held open, blocks for the transaction's full remaining duration (confirmed
  empirically: a deliberately-held-open transaction blocked a concurrent reader for the full hold
  time). This was judged acceptable as-is (no RCSI, no MERGE-based rewrite) given the small measured
  duration; revisit only if production numbers show real contention.

**Manual backfill.** `POST /api/v1/admin/dashboard-rollup/backfill` (`SuperAdmin` only, `Kind`
validated against the enum so an invalid value is a clean 400, not an unhandled 500) calls
`BackfillOneChunkAsync` up to `MaxChunks` times (default 1, capped at 30 per call), each chunk
`BackfillChunkDays` (default 1) wide, resuming from the organization's current watermark every time
- safe to call repeatedly/resume after an interruption, and the only way historical data (older than
the regular cycle's lookback) ever enters the rollup. `GET
/api/v1/admin/dashboard-rollup/watermarks` reports current progress per rollup kind.

**Retention guard.** `AuditRetentionWorker` never purges a raw `AuditEvents` row whose
`OccurredAtUtc` is newer than the organization's `HourlyOccurred`/`EmployeeDaily` watermark (the
lower of the two) - an organization missing either watermark has nothing purged regardless of age.
In practice this rarely narrows anything, since `AuditRetention:RetentionDays` (default `0`,
disabled) is normally far larger than the rollup's own lookback windows; it only matters for an
aggressively short retention setting, or before the rollup has ever run.

**Covering index.** `scripts/AuditEvents_CoveringIndex.sql` extends the existing
`IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc` index (via `WITH (DROP_EXISTING = ON)`, not a second
index) with `INCLUDE (DecisionId, ActionKey, EmployeeId, ReasonCodeId)`, so the rollup's own
aggregation queries (and the raw path, when `UseRollup=false`) don't fall back to the clustered index
for those columns. Deliberately **not** an EF migration - `MigrateAsync` runs automatically at app
startup, and building/rebuilding an index on a table this size must happen in a planned maintenance
window. Run it by hand; it ships both an offline (`DROP_EXISTING=ON`) form - the only one usable on
Standard/Express edition, and the one actually tested against the local database - and a commented-out
`ONLINE=ON` form for Enterprise/Developer edition.

**Enabling in production** (see the script and `DashboardRollupAdminController` for the pieces):
1. Deploy the code with both flags `false` (this is the default - no behavior change).
2. Let the migration create the two rollup tables (additive only, no data).
3. Run `scripts/AuditEvents_CoveringIndex.sql` by hand in a maintenance window.
4. Run the manual backfill for every organization until `watermarks` shows no more remaining.
5. Set `RefreshEnabled=true` and let the worker run for at least one full day; compare its numbers
   against the raw path for that day.
6. Only then set `UseRollup=true`.

**Rolling back**: set `UseRollup=false` (and, if desired, `RefreshEnabled=false`). Nothing about the
raw tables or the raw query path is ever changed by this feature, so rollback is instant and has no
data-loss risk.

## Future improvements (proposed, not implemented)

- **Possible index**: ~~`/kpis`, `/breakdown` and `/recent-events` all filter `dbo.AuditEvents` by
  `OrganizationId` + a `ReceivedAtUtc` range~~ - **implemented**, see "Rollup" above and
  `scripts/AuditEvents_CoveringIndex.sql`. The index now also covers `EmployeeId`/`ReasonCodeId` (not
  just `DecisionId`/`ActionKey`), since the rollup's own aggregation needs both.
