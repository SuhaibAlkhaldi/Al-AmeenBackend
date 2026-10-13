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

## Future improvements (proposed, not implemented)

- **Possible index**: `/kpis`, `/breakdown` and `/recent-events` all filter `dbo.AuditEvents` by
  `OrganizationId` + a `ReceivedAtUtc` range (already covered by the existing
  `IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc` index from migration
  `AddAuditEventsOrgReceivedOccurredIndex`), then aggregate/select `DecisionId` (and, for
  `/recent-events`, `ActionKey`) per matching row. A covering index such as
  `(OrganizationId, ReceivedAtUtc) INCLUDE (DecisionId, ActionKey)` could save the lookup back to
  the clustered index for those columns on larger datasets. Deliberately not added here - no
  migration was created. Revisit this only if real production numbers after deploy actually show
  it's needed; it's a separate, follow-up task.
