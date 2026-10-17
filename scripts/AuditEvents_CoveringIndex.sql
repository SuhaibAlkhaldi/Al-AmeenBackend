/*
 dashboard-rollup: covering index for dbo.AuditEvents, to speed up:
   - DashboardRollupRefreshService's own aggregation queries (runs every
     DashboardRollup:RefreshIntervalSeconds, scanning the last LateEventLookbackHours of raw
     AuditEvents, grouped by DecisionId/ActionKey/ReasonCodeId) - this is now the MAIN beneficiary
     once the rollup feature is live, since it runs far more often than any one dashboard page load.
   - the raw query path (GetSummaryAsync, and kpis/breakdown/weekly-trend/risk whenever
     DashboardRollup:UseRollup=false) - the original motivation noted in DASHBOARD_ENDPOINTS.md's
     "Future improvements" section.

 NOT run as part of this repo's EF migrations (deliberately - see the dashboard-rollup plan item
 4): applying `dotnet ef database migrate` happens automatically on app startup in this project,
 and a schema change of this size on a table with tens of millions of rows must happen in a
 planned maintenance window, not block (or race) app startup. Run this by hand, once, with a DBA
 or ops person watching.

 ============================================================================================
 DUPLICATE CHECK (done before writing this script - do not skip this check again before running
 it against a database you haven't already checked):

   SELECT i.name, i.type_desc,
          STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS key_columns,
          i.is_included_column
   FROM sys.indexes i
   JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
   JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
   WHERE i.object_id = OBJECT_ID('dbo.AuditEvents')
   GROUP BY i.name, i.type_desc, i.is_included_column;

 Result on this project's own dev database: an index named IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc
 ALREADY EXISTS, keyed on (OrganizationId ASC, ReceivedAtUtc DESC, OccurredAtUtc DESC) with NO
 included columns (added by migration 20261008155900_AddAuditEventsOrgReceivedOccurredIndex).
 Its key columns are an exact prefix match for what was asked for here - creating a SECOND,
 separate index with the same leading columns would be pure duplication (extra storage, extra
 write cost, and SQL Server would almost certainly only ever pick one of the two anyway). The
 correct change is therefore to EXTEND that existing index with the needed INCLUDE columns, not
 add a new one - see the CREATE INDEX below, which uses WITH (DROP_EXISTING = ON) for exactly
 that: one atomic statement that redefines the same index in place (same name, same key columns,
 same sort order) rather than a separate DROP followed by a separate CREATE (which would leave a
 window where the index doesn't exist at all between the two statements).

 Not included in the check above but worth noting for a possible FOLLOW-UP (not done here, not
 asked for): the OccurredAtUtc-based queries (weekly-trend/risk's raw path, and this rollup's own
 HourlyOccurred/EmployeeDaily aggregation) filter on OccurredAtUtc as the LEADING time column, not
 ReceivedAtUtc - this index's leading key is ReceivedAtUtc, so it does not help those queries as
 directly. The existing IX_AuditEvents_Organization_Occurred (OrganizationId, OccurredAtUtc) index
 is available for those and is NOT touched by this script, but it has no INCLUDE columns either.
 A mirror-image covering index - (OrganizationId, OccurredAtUtc) INCLUDE (DecisionId, ActionKey,
 EmployeeId, ReasonCodeId) - would do the same job for that side. Deliberately left as a follow-up
 decision, not bundled into this script, so its own write-cost impact can be measured and decided
 on separately.
 ============================================================================================

 WRITE-COST IMPACT: every INSERT into AuditEvents (the agent ingestion path -
 AgentAuditEventService, high-frequency) must now also maintain 5 included columns instead of 0
 on this one index - a small per-row cost, but this table is write-heavy, so it was measured
 directly (not assumed) - see the dashboard-rollup report's "write-cost" table for the actual
 before/after INSERT timing on this project's own local database.

 STORAGE IMPACT: DecisionId (int, 4 bytes) + ActionKey (nvarchar(100), up to 200 bytes) +
 EmployeeId (uniqueidentifier, 16 bytes) + ReasonCodeId (int, nullable, 4 bytes) added to every
 leaf-level row of this index - bounded, not multiplied by anything beyond the existing row count.

 EDITION: ONLINE = ON requires Enterprise or Developer edition. This project's own local dev
 database is SQL Server 2022 EXPRESS, where ONLINE = ON is rejected outright - use the
 OFFLINE version (the one actually run in this script) to test locally. The production
 database's edition was not yet known when this script was written - check
 SELECT SERVERPROPERTY('Edition') there first, and use the ONLINE version at the bottom of this
 file ONLY if that comes back Enterprise or Developer.
*/

-- ============================================================================================
-- OFFLINE version - works on every SQL Server edition (Express/Standard/Enterprise/Developer).
-- Blocks reads/writes to dbo.AuditEvents for the duration of the rebuild (a schema modification
-- lock) - run during a maintenance window, not during live traffic, on a table this size.
-- ============================================================================================
CREATE NONCLUSTERED INDEX [IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc]
    ON [dbo].[AuditEvents] ([OrganizationId] ASC, [ReceivedAtUtc] DESC, [OccurredAtUtc] DESC)
    INCLUDE ([DecisionId], [ActionKey], [EmployeeId], [ReasonCodeId])
    WITH (DROP_EXISTING = ON, MAXDOP = 0);
GO

-- ============================================================================================
-- ONLINE version - Enterprise/Developer edition ONLY (confirm with
-- SELECT SERVERPROPERTY('Edition') first - this statement is rejected outright on
-- Standard/Express). Keeps dbo.AuditEvents readable and writable throughout the rebuild, at the
-- cost of extra tempdb/log usage and a longer wall-clock rebuild time than the offline version.
-- Commented out - uncomment and run THIS instead of the block above if production is a
-- supporting edition.
-- ============================================================================================
-- CREATE NONCLUSTERED INDEX [IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc]
--     ON [dbo].[AuditEvents] ([OrganizationId] ASC, [ReceivedAtUtc] DESC, [OccurredAtUtc] DESC)
--     INCLUDE ([DecisionId], [ActionKey], [EmployeeId], [ReasonCodeId])
--     WITH (DROP_EXISTING = ON, ONLINE = ON, MAXDOP = 0);
-- GO

-- ============================================================================================
-- ROLLBACK - reverts to the original index definition (no INCLUDE columns), if this change ever
-- needs to be undone. Also offline-only on Standard/Express; add ONLINE = ON for Enterprise/
-- Developer the same way as above.
-- ============================================================================================
-- CREATE NONCLUSTERED INDEX [IX_AuditEvents_Org_ReceivedAtUtc_OccurredAtUtc]
--     ON [dbo].[AuditEvents] ([OrganizationId] ASC, [ReceivedAtUtc] DESC, [OccurredAtUtc] DESC)
--     WITH (DROP_EXISTING = ON, MAXDOP = 0);
-- GO
