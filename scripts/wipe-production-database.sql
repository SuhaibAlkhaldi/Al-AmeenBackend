/*
================================================================================
 wipe-production-database.sql
================================================================================
 Purpose : Wipe every row from the DLPSystem production database EXCEPT the
           admin@dlp SuperAdmin user, its own Organization, and every small
           fixed lookup/enumeration table the app needs to boot and function
           at all (see @LookupTablesToPreserve below - Roles, UserStatuses,
           DeviceStatuses, PermissionActions, __EFMigrationsHistory, etc.).
           After this runs you re-provision fresh devices from a clean slate.

 Target  : Server 161.97.90.171,8797, Database DLPSystem (from
           appsettings.Production.json). Run this with sqlcmd or SSMS
           connected to that server, NOT against Development/local by mistake.

 IMPORTANT - READ BEFORE RUNNING
 --------------------------------
 1. This is DESTRUCTIVE and irreversible except by restoring the backup this
    script takes. Do not skip step 1 (backup) and do not delete the .bak file
    afterwards until you've confirmed the re-provisioned system works.
 2. This was prepared by Claude per your request but is NOT executed by
    Claude - you must run it yourself after reviewing it.
 3. __EFMigrationsHistory and every small fixed lookup/enumeration table the
    app depends on everywhere (Roles, UserTypes, UserStatuses, EmployeeStatuses,
    DeviceStatuses, PermissionDecisions, PermissionGrantTypes,
    PermissionSubjectTypes, PermissionActionCategories, PermissionActions,
    PermissionRequestStatuses, PermissionRequestReviewDecisions, AuditDecisions,
    AuditEventTypes, AuditReasonCodes, AgentCommandStatuses, AlertLevels,
    AlertStatuses, DemoRequestStatuses - see @LookupTablesToPreserve below,
    kept in sync with every table DatabaseSeeder.cs seeds) are intentionally
    left completely untouched. These are reference data, not device/tenant
    data - wiping them breaks the app for the surviving admin@dlp account too,
    not just "remove noise". __EFMigrationsHistory specifically is EF Core's
    own bookkeeping table, never application data: if it's wiped, the next
    app startup thinks no migration was ever applied and tries to
    CREATE TABLE everything from scratch, which crashes because the tables
    physically still exist (only their rows were deleted, never dropped) -
    confirmed live (2026-08-24), this took the whole backend down in a crash
    loop until __EFMigrationsHistory was manually repopulated.
 4. Every other table (Devices, DeviceCredentials, PermissionGrants,
    AuditEvents, Alerts, PolicyVersions, AgentEnrollmentTokens, etc.) is fully
    wiped, including Organizations except the one row admin@dlp itself belongs
    to (kept so the FK on Users.OrganizationId stays valid and the admin can
    still log in against a real org).
 5. IDENTITY/sequence counters are NOT reset - new rows will simply continue
    numbering from wherever they left off. This is harmless and intentional;
    remove the comment on the DBCC CHECKIDENT block near the bottom if you'd
    rather start integer IDs back at 1 (most tables use GUID PKs anyway).
 6. The script disables ALL foreign-key constraints, deletes, then
    re-enables and re-validates them (WITH CHECK). If re-validation fails
    at the end, something was missed - the transaction rolls back and NOTHING
    is committed. You'll see the FK violation error naming the offending
    table/constraint.

 Recommended run:
   sqlcmd -S 161.97.90.171,8797 -U dlp_app_user -P "<password>" -d DLPSystem -i wipe-production-database.sql -o wipe-production-database.log
================================================================================
*/

-- Required for the DELETEs run via sp_MSforeachtable's dynamic SQL to work against any
-- table that has a filtered index / indexed view / computed column - without these ON,
-- SQL Server rejects the DELETE with error 1934. Must be set before anything else runs.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;  -- any error auto-rolls-back the whole transaction, no partial wipe

-- Use the SQL Server instance's own configured default backup directory instead of a
-- hardcoded path - a hardcoded C:\Backups almost never exists on a remote production
-- box you don't have filesystem access to, and this folder is guaranteed to exist
-- because the instance is already configured to use it.
DECLARE @DefaultBackupDir NVARCHAR(500) = CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(500));
IF RIGHT(@DefaultBackupDir, 1) <> N'\'
    SET @DefaultBackupDir = @DefaultBackupDir + N'\';
DECLARE @BackupPath NVARCHAR(500) =
    @DefaultBackupDir + N'DLPSystem_before_wipe_' + FORMAT(SYSUTCDATETIME(), 'yyyyMMdd_HHmmss') + N'.bak';
DECLARE @AdminEmail NVARCHAR(255) = N'admin@dlp';

-- Every table that must survive completely untouched: EF Core's own migration bookkeeping table,
-- plus every lookup/enumeration table DatabaseSeeder.cs seeds (kept in sync with that file's Seed()
-- method - if a future migration adds a new lookup table there, add its name here too). Users and
-- Organizations are NOT listed here - they're excluded from the blanket delete in Step 4 but still
-- get trimmed down to just the admin's own row in Step 5, so they can't just be skipped outright.
DECLARE @LookupTablesToPreserve TABLE (Name SYSNAME PRIMARY KEY);
INSERT INTO @LookupTablesToPreserve (Name) VALUES
    (N'__EFMigrationsHistory'),
    (N'Roles'), (N'UserTypes'), (N'UserStatuses'), (N'EmployeeStatuses'), (N'DeviceStatuses'),
    (N'PermissionDecisions'), (N'PermissionGrantTypes'), (N'PermissionSubjectTypes'),
    (N'PermissionActionCategories'), (N'PermissionActions'),
    (N'PermissionRequestStatuses'), (N'PermissionRequestReviewDecisions'),
    (N'AuditDecisions'), (N'AuditEventTypes'), (N'AuditReasonCodes'),
    (N'AgentCommandStatuses'), (N'AlertLevels'), (N'AlertStatuses'),
    (N'DemoRequestStatuses');

PRINT N'=== Step 0: Safety check - confirm ' + @AdminEmail + N' exists before doing anything destructive ===';
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @AdminEmail)
BEGIN
    RAISERROR(N'Aborting: no user with Email = %s was found. Nothing was changed.', 16, 1, @AdminEmail);
    RETURN;
END

PRINT N'=== Step 1: Mandatory full backup to ' + @BackupPath + N' ===';
BACKUP DATABASE [DLPSystem]
TO DISK = @BackupPath
WITH INIT, COMPRESSION, STATS = 10;

PRINT N'=== Step 2: Capture the rows that must survive (admin user + its org/role/status/type) ===';
DECLARE @AdminUserId UNIQUEIDENTIFIER, @AdminOrgId UNIQUEIDENTIFIER;
SELECT
    @AdminUserId = Id,
    @AdminOrgId  = OrganizationId
FROM dbo.Users
WHERE Email = @AdminEmail;

IF @AdminUserId IS NULL OR @AdminOrgId IS NULL
BEGIN
    RAISERROR(N'Aborting: could not resolve admin user id / organization id.', 16, 1);
    RETURN;
END

PRINT N'    Admin user id: ' + CAST(@AdminUserId AS NVARCHAR(50));
PRINT N'    Admin org id : ' + CAST(@AdminOrgId AS NVARCHAR(50));

BEGIN TRANSACTION WipeProduction;

BEGIN TRY

    -- Note: deliberately NOT using sp_MSforeachtable here. That system proc was compiled
    -- with QUOTED_IDENTIFIER OFF baked in, so dynamic SQL run through it ignores whatever
    -- we SET at the top of this script and throws error 1934 on any table with a filtered
    -- index / computed column / indexed view - confirmed live (2026-08-24). Building the
    -- statements ourselves from sys.tables and running them via EXEC() in our own batch
    -- uses this session's real SET options instead.
    DECLARE @sql NVARCHAR(MAX);

    PRINT N'=== Step 3: Disable all foreign-key constraints (so delete order does not matter) ===';
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' NOCHECK CONSTRAINT ALL;' + CHAR(10)
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id;
    EXEC(@sql);

    PRINT N'=== Step 4: Delete every table except the lookup tables the admin needs to keep working ===';
    -- Everything in @LookupTablesToPreserve is skipped entirely (see header note #3).
    -- Users and Organizations are also skipped here and handled precisely in step 5,
    -- since those two need a WHERE clause rather than a full wipe.
    SET @sql = N'';
    SELECT @sql = @sql + N'DELETE FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(10)
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE t.name NOT IN (N'Users', N'Organizations')
      AND t.name NOT IN (SELECT Name FROM @LookupTablesToPreserve);
    EXEC(@sql);

    PRINT N'=== Step 5: Trim Organizations and Users down to just the admin''s own rows ===';
    DELETE FROM dbo.Organizations WHERE Id <> @AdminOrgId;
    DELETE FROM dbo.Users WHERE Id <> @AdminUserId;

    PRINT N'=== Step 6: Re-enable and re-validate all foreign-key constraints ===';
    SET @sql = N'';
    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
    FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id;
    EXEC(@sql);

    -- Optional: reset integer IDENTITY counters back to their seed. Left commented out
    -- by default - most tables use GUID primary keys so this is cosmetic, not required
    -- for the app to work. Uncomment only if you specifically want IDs to restart at 1.
    -- SET @sql = N'';
    -- SELECT @sql = @sql + N'IF OBJECTPROPERTY(OBJECT_ID(''' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N'''), ''TableHasIdentity'') = 1 DBCC CHECKIDENT (''' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N''', RESEED, 0);' + CHAR(10)
    -- FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id;
    -- EXEC(@sql);

    COMMIT TRANSACTION WipeProduction;
    PRINT N'=== Done. Committed. Only ' + @AdminEmail + N' (and its Organization) remain, plus every table in @LookupTablesToPreserve (including __EFMigrationsHistory). ===';
    PRINT N'=== Backup file: ' + @BackupPath + N' - keep this until you have verified the app works. ===';
    PRINT N'=== Next step: recycle the Al-AmeenBackend IIS app pool once so DatabaseSeeder re-populates any lookup table that legitimately gained new rows since this script was last updated. ===';

END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION WipeProduction;

    PRINT N'=== FAILED - rolled back, nothing was changed except the backup file already written in step 1. ===';
    PRINT N'Error ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)) + N': ' + ERROR_MESSAGE();
    THROW;
END CATCH
