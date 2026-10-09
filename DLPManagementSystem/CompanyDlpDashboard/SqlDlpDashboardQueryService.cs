using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Linq;

namespace DLPManagementSystem.CompanyDlpDashboard;

public sealed class SqlDlpDashboardQueryService : IDlpDashboardQueryService
{
    private readonly string _connectionString;
    private readonly DlpDashboardOptions _options;

    public SqlDlpDashboardQueryService(
        IConfiguration configuration,
        IOptions<DlpDashboardOptions> options)
    {
        _options = options.Value;

        var connectionStringName = string.IsNullOrWhiteSpace(_options.ConnectionStringName)
            ? "DefaultConnection"
            : _options.ConnectionStringName;

        _connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' was not found.");
    }

    // v1 risk-score constants (documented starting point, see ComputeRiskScores) — adjustable later.
    private const int RiskWindowDays = 30;
    private const int RiskHighThreshold = 70;
    private const int RiskMediumThreshold = 40;
    private const int HeartbeatFreshMinutes = 5;
    private const int TopRiskyUserCount = 10;

    // --- The ten dashboard queries, as one multi-statement batch -----------------------------------
    //
    // Phase 1 batching (2026-10-08, see the dashboard-slowness plan): these used to run as ten
    // separate ExecuteReaderAsync round trips on one connection — correct, but each round trip pays
    // full network latency to the SQL Server, which outside local dev is a remote shared instance.
    // A live browser test under concurrent page load (dashboard + People + Devices + Alerts + Audit
    // Events opened together) showed real benefit from cutting that to one round trip. Each query's
    // own text, parameters and result-row shape are unchanged from before batching — only how they're
    // sent and read back changed. GetSummaryAsync concatenates these in order and reads them back with
    // NextResultAsync() in the same order; keep both in sync if a query is added, removed or reordered.
    //
    // Shared parameters across all ten: @OrganizationId; @FromUtc/@ToUtc (the summary's own from/to);
    // @WeeklyFromUtc/@WeeklyToUtc (fixed rolling 28-day window); @RiskFromUtc/@RiskToUtc (fixed rolling
    // 30-day window); @FreshSinceUtc (heartbeat freshness cutoff). All eight are bound once in
    // GetSummaryAsync and are in scope for every statement in the batch.

    private const string TotalsSql = @"
SELECT
    COUNT_BIG(1) AS TotalEvents,
    SUM(CASE WHEN ad.Name = N'Allow' THEN 1 ELSE 0 END) AS AllowedEvents,
    SUM(CASE WHEN ad.Name = N'Block' THEN 1 ELSE 0 END) AS BlockedEvents,
    SUM(CASE WHEN ad.Name IN (N'AuditOnly', N'Audit') THEN 1 ELSE 0 END) AS AuditOnlyEvents,
    SUM(CASE WHEN ad.Name = N'Error' THEN 1 ELSE 0 END) AS ErrorEvents,
    COUNT(DISTINCT ae.ActionKey) AS UniqueActions,
    MAX(ae.PolicyVersion) AS PolicyVersion,
    MAX(ae.ReceivedAtUtc) AS LastReceivedAtUtc
FROM dbo.AuditEvents ae
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
WHERE ae.OrganizationId = @OrganizationId
  AND ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc;
";

    // Excludes the "System" category (policy.apply/agent.session housekeeping) — those default to
    // Allow and aren't user-facing violations, so a Blocked row there would be unexpected, not a
    // channel to chart. See DlpChannelBreakdownItemDto for the reconciliation note.
    private const string ActionBreakdownSql = @"
SELECT TOP (30)
    ae.ActionKey,
    COUNT_BIG(1) AS Total,
    SUM(CASE WHEN ad.Name = N'Allow' THEN 1 ELSE 0 END) AS Allowed,
    SUM(CASE WHEN ad.Name = N'Block' THEN 1 ELSE 0 END) AS Blocked,
    SUM(CASE WHEN ad.Name = N'Error' THEN 1 ELSE 0 END) AS Errors,
    MAX(ae.ReceivedAtUtc) AS LastReceivedAtUtc
FROM dbo.AuditEvents ae
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
WHERE ae.OrganizationId = @OrganizationId
  AND ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc
GROUP BY ae.ActionKey
ORDER BY COUNT_BIG(1) DESC, ae.ActionKey ASC;
";

    private const string ReasonBreakdownSql = @"
SELECT TOP (15)
    COALESCE(arc.Code, N'Unknown') AS ReasonCode,
    COUNT_BIG(1) AS Total
FROM dbo.AuditEvents ae
LEFT JOIN dbo.AuditReasonCodes arc ON arc.Id = ae.ReasonCodeId
WHERE ae.OrganizationId = @OrganizationId
  AND ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc
GROUP BY COALESCE(arc.Code, N'Unknown')
ORDER BY COUNT_BIG(1) DESC;
";

    private const string RecentEventsSql = @"
SELECT TOP (5)
    ae.ActionKey,
    aet.DisplayName AS EventType,
    ae.Username,
    ae.UserSid,
    CONVERT(nvarchar(50), ae.DeviceId) AS DeviceId,
    ae.MetadataJson,
    ad.Name AS Decision,
    arc.Code AS ReasonCode,
    ae.PolicyVersion,
    ae.OccurredAtUtc,
    ae.ReceivedAtUtc
FROM dbo.AuditEvents ae
LEFT JOIN dbo.AuditEventTypes aet ON aet.Id = ae.EventTypeId
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
LEFT JOIN dbo.AuditReasonCodes arc ON arc.Id = ae.ReasonCodeId
WHERE ae.OrganizationId = @OrganizationId
  AND ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc
ORDER BY ae.ReceivedAtUtc DESC, ae.OccurredAtUtc DESC;
";

    private const string ChannelBreakdownSql = @"
SELECT pac.Name AS Category, COUNT_BIG(1) AS Blocked
FROM dbo.AuditEvents ae
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
LEFT JOIN dbo.PermissionActions pa ON pa.[Key] = ae.ActionKey
LEFT JOIN dbo.PermissionActionCategories pac ON pac.Id = pa.CategoryId
WHERE ae.OrganizationId = @OrganizationId
  AND ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc
  AND ad.Name = N'Block'
  AND pac.Name IS NOT NULL
  AND pac.Name <> N'System'
GROUP BY pac.Name
ORDER BY COUNT_BIG(1) DESC;
";

    // Fixed rolling 4-week window (independent of the summary's from/to toggle). Bucketed in memory
    // rather than in T-SQL date arithmetic for clarity/correctness — the row volume here is bounded by
    // definition (28 days of events) so this is cheap.
    private const string WeeklyTrendSql = @"
SELECT ae.OccurredAtUtc, ad.Name AS DecisionName
FROM dbo.AuditEvents ae
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
WHERE ae.OrganizationId = @OrganizationId
  AND ae.OccurredAtUtc >= @WeeklyFromUtc
  AND ae.OccurredAtUtc < @WeeklyToUtc;
";

    private const string PermissionRequestCountsSql = @"
SELECT prs.Name AS StatusName, COUNT_BIG(1) AS Cnt
FROM dbo.PermissionRequests pr
INNER JOIN dbo.PermissionRequestStatuses prs ON prs.Id = pr.StatusId
WHERE pr.OrganizationId = @OrganizationId
GROUP BY prs.Name;
";

    // v1 risk score definition — not an established/audited metric, a documented first-pass starting
    // point:
    //   riskScore(employee) = round(100 * blockCount(employee, last 30 days) / max(blockCount) across
    //   the org in the same window). If nobody has any blocked events, everyone scores 0.
    //   riskLevel: >=70 High, >=40 Medium, else Low (RiskHighThreshold/RiskMediumThreshold above).
    private const string RiskBlockCountSql = @"
SELECT e.Id AS EmployeeId, e.DisplayName, d.Name AS DepartmentName,
       COUNT(CASE WHEN ad.Name = N'Block' THEN 1 END) AS BlockCount
FROM dbo.Employees e
LEFT JOIN dbo.Departments d ON d.Id = e.DepartmentId
LEFT JOIN dbo.AuditEvents ae ON ae.EmployeeId = e.Id
    AND ae.OccurredAtUtc >= @RiskFromUtc AND ae.OccurredAtUtc < @RiskToUtc
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
WHERE e.OrganizationId = @OrganizationId
GROUP BY e.Id, e.DisplayName, d.Name;
";

    private const string RiskFactorSql = @"
SELECT ae.EmployeeId, pac.Name AS Category, COUNT_BIG(1) AS Cnt
FROM dbo.AuditEvents ae
INNER JOIN dbo.Employees e ON e.Id = ae.EmployeeId AND e.OrganizationId = @OrganizationId
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
LEFT JOIN dbo.PermissionActions pa ON pa.[Key] = ae.ActionKey
LEFT JOIN dbo.PermissionActionCategories pac ON pac.Id = pa.CategoryId
WHERE ae.OccurredAtUtc >= @RiskFromUtc AND ae.OccurredAtUtc < @RiskToUtc
  AND ad.Name = N'Block'
  AND ae.EmployeeId IS NOT NULL
  AND pac.Name IS NOT NULL
  AND pac.Name <> N'System'
GROUP BY ae.EmployeeId, pac.Name;
";

    private const string EndpointAgentStatusSql = @"
SELECT
    COUNT(1) AS EnrolledDeviceCount,
    SUM(CASE WHEN d.LastSeenAtUtc >= @FreshSinceUtc THEN 1 ELSE 0 END) AS HeartbeatFreshDeviceCount
FROM dbo.Devices d
WHERE d.OrganizationId = @OrganizationId;
";

    private const string BatchSql =
        TotalsSql + ActionBreakdownSql + ReasonBreakdownSql + RecentEventsSql + ChannelBreakdownSql +
        WeeklyTrendSql + PermissionRequestCountsSql + RiskBlockCountSql + RiskFactorSql + EndpointAgentStatusSql;

    public async Task<DlpDashboardSummaryDto> GetSummaryAsync(
        Guid organizationId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var to = (toUtc ?? nowUtc).ToUniversalTime();
        var from = (fromUtc ?? to.AddHours(-Math.Max(_options.DefaultLookbackHours, 1))).ToUniversalTime();

        if (from >= to)
        {
            from = to.AddHours(-24);
        }

        var maxRange = TimeSpan.FromDays(Math.Max(_options.MaximumLookbackDays, 1));
        if (to - from > maxRange)
        {
            from = to.Subtract(maxRange);
        }

        var weeklyFrom = nowUtc.AddDays(-28);
        var riskFrom = nowUtc.AddDays(-RiskWindowDays);
        var freshSince = nowUtc.AddMinutes(-HeartbeatFreshMinutes);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = BatchSql;
        // 90s, not the 30s each of the ten queries used individually before batching: CommandTimeout
        // applies to the whole batch now, not per-statement, so under load it's possible for several
        // statements to each wait a few seconds for a memory grant (never individually near 30s) while
        // the sum across all ten exceeds a 30s budget - observed live under heavy concurrent load after
        // batching landed (see the dashboard-slowness plan's Step 7 measurement notes).
        command.CommandTimeout = 90;
        command.Parameters.Add(new SqlParameter("@OrganizationId", SqlDbType.UniqueIdentifier) { Value = organizationId });
        command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTimeOffset) { Value = from });
        command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTimeOffset) { Value = to });
        command.Parameters.Add(new SqlParameter("@WeeklyFromUtc", SqlDbType.DateTimeOffset) { Value = weeklyFrom });
        command.Parameters.Add(new SqlParameter("@WeeklyToUtc", SqlDbType.DateTimeOffset) { Value = nowUtc });
        command.Parameters.Add(new SqlParameter("@RiskFromUtc", SqlDbType.DateTimeOffset) { Value = riskFrom });
        command.Parameters.Add(new SqlParameter("@RiskToUtc", SqlDbType.DateTimeOffset) { Value = nowUtc });
        command.Parameters.Add(new SqlParameter("@FreshSinceUtc", SqlDbType.DateTimeOffset) { Value = freshSince });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // Read order must match BatchSql's concatenation order exactly.
        var (totalsDto, policyVersion) = await ReadTotalsAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var actionBreakdown = await ReadActionBreakdownAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var reasonBreakdown = await ReadReasonBreakdownAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var recentEvents = await ReadRecentEventsAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var channelBreakdown = await ReadChannelBreakdownAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var weeklyTrend = await ReadWeeklyTrendAsync(reader, nowUtc, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var permissionRequestCounts = await ReadPermissionRequestCountsAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var employeeRows = await ReadRiskBlockCountsAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var factorsByEmployee = await ReadRiskFactorsAsync(reader, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        var endpointAgents = await ReadEndpointAgentStatusAsync(reader, cancellationToken);

        var (topRiskyUsers, highRiskUserCount, riskByDepartment) = ComputeRiskScores(employeeRows, factorsByEmployee);

        return new DlpDashboardSummaryDto(
            DateTimeOffset.UtcNow,
            from,
            to,
            policyVersion,
            totalsDto,
            actionBreakdown,
            reasonBreakdown,
            recentEvents,
            permissionRequestCounts,
            weeklyTrend,
            channelBreakdown,
            topRiskyUsers,
            riskByDepartment,
            highRiskUserCount,
            endpointAgents);
    }

    private static async Task<(DlpDashboardTotalsDto Totals, long? PolicyVersion)> ReadTotalsAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
        {
            return (new DlpDashboardTotalsDto(0, 0, 0, 0, 0, 0, null), null);
        }

        var dto = new DlpDashboardTotalsDto(
            GetLong(reader, "TotalEvents"),
            GetLong(reader, "AllowedEvents"),
            GetLong(reader, "BlockedEvents"),
            GetLong(reader, "AuditOnlyEvents"),
            GetLong(reader, "ErrorEvents"),
            GetInt(reader, "UniqueActions"),
            GetNullableDateTimeOffset(reader, "LastReceivedAtUtc"));

        return (dto, GetNullableLong(reader, "PolicyVersion"));
    }

    private static async Task<IReadOnlyList<DlpActionBreakdownItemDto>> ReadActionBreakdownAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new List<DlpActionBreakdownItemDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DlpActionBreakdownItemDto(
                GetString(reader, "ActionKey"),
                GetLong(reader, "Total"),
                GetLong(reader, "Allowed"),
                GetLong(reader, "Blocked"),
                GetLong(reader, "Errors"),
                GetNullableDateTimeOffset(reader, "LastReceivedAtUtc")));
        }

        return result;
    }

    private static async Task<IReadOnlyList<DlpReasonBreakdownItemDto>> ReadReasonBreakdownAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new List<DlpReasonBreakdownItemDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DlpReasonBreakdownItemDto(
                GetString(reader, "ReasonCode"),
                GetLong(reader, "Total")));
        }

        return result;
    }

    private static async Task<IReadOnlyList<DlpRecentAuditEventDto>> ReadRecentEventsAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new List<DlpRecentAuditEventDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var metadataJson = GetNullableString(reader, "MetadataJson");

            result.Add(new DlpRecentAuditEventDto(
                GetString(reader, "ActionKey"),
                GetNullableString(reader, "EventType"),
                GetNullableString(reader, "Username"),
                GetNullableString(reader, "UserSid"),
                GetNullableString(reader, "DeviceId"),
                DlpMetadataDetailsBuilder.Build(metadataJson),
                GetNullableString(reader, "Decision"),
                GetNullableString(reader, "ReasonCode"),
                GetNullableLong(reader, "PolicyVersion"),
                GetDateTimeOffset(reader, "OccurredAtUtc"),
                GetDateTimeOffset(reader, "ReceivedAtUtc")));
        }

        return result;
    }

    private static async Task<IReadOnlyList<DlpChannelBreakdownItemDto>> ReadChannelBreakdownAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        var result = new List<DlpChannelBreakdownItemDto>();

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DlpChannelBreakdownItemDto(
                GetString(reader, "Category"),
                GetLong(reader, "Blocked")));
        }

        return result;
    }

    private static async Task<IReadOnlyList<DlpWeeklyTrendPointDto>> ReadWeeklyTrendAsync(
        SqlDataReader reader, DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        var rows = new List<(DateTimeOffset OccurredAtUtc, string? DecisionName)>();

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((GetDateTimeOffset(reader, "OccurredAtUtc"), GetNullableString(reader, "DecisionName")));
        }

        var points = new List<DlpWeeklyTrendPointDto>();

        for (var i = 3; i >= 0; i--)
        {
            var bucketEnd = nowUtc.AddDays(-7 * i);
            var bucketStart = bucketEnd.AddDays(-7);

            var bucketRows = rows.Where(r => r.OccurredAtUtc >= bucketStart && r.OccurredAtUtc < bucketEnd).ToList();
            var blocked = bucketRows.Count(r => r.DecisionName == "Block");
            var violations = bucketRows.Count(r => r.DecisionName is "Block" or "AuditOnly" or "Audit");

            points.Add(new DlpWeeklyTrendPointDto(bucketStart, blocked, violations));
        }

        return points;
    }

    private static async Task<DlpPermissionRequestCountsDto> ReadPermissionRequestCountsAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        long pending = 0, approved = 0, rejected = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            var status = GetString(reader, "StatusName");
            var count = GetLong(reader, "Cnt");

            // "Pending" = anything still awaiting a decision (Submitted or UnderReview).
            switch (status)
            {
                case "Submitted":
                case "UnderReview":
                    pending += count;
                    break;
                case "Approved":
                    approved += count;
                    break;
                case "Rejected":
                    rejected += count;
                    break;
            }
        }

        return new DlpPermissionRequestCountsDto(pending, approved, rejected);
    }

    private static async Task<List<(Guid EmployeeId, string DisplayName, string? DepartmentName, long BlockCount)>> ReadRiskBlockCountsAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        var employeeRows = new List<(Guid EmployeeId, string DisplayName, string? DepartmentName, long BlockCount)>();

        while (await reader.ReadAsync(cancellationToken))
        {
            employeeRows.Add((
                reader.GetGuid(reader.GetOrdinal("EmployeeId")),
                GetString(reader, "DisplayName"),
                GetNullableString(reader, "DepartmentName"),
                GetLong(reader, "BlockCount")));
        }

        return employeeRows;
    }

    private static async Task<Dictionary<Guid, List<DlpRiskFactorDto>>> ReadRiskFactorsAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        var factorsByEmployee = new Dictionary<Guid, List<DlpRiskFactorDto>>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var employeeId = reader.GetGuid(reader.GetOrdinal("EmployeeId"));
            var factor = new DlpRiskFactorDto(GetString(reader, "Category"), GetLong(reader, "Cnt"));

            if (!factorsByEmployee.TryGetValue(employeeId, out var list))
            {
                list = new List<DlpRiskFactorDto>();
                factorsByEmployee[employeeId] = list;
            }

            list.Add(factor);
        }

        return factorsByEmployee;
    }

    private static async Task<DlpEndpointAgentStatusDto> ReadEndpointAgentStatusAsync(
        SqlDataReader reader, CancellationToken cancellationToken)
    {
        var enrolledCount = 0;
        var freshCount = 0;

        if (await reader.ReadAsync(cancellationToken))
        {
            enrolledCount = GetInt(reader, "EnrolledDeviceCount");
            freshCount = GetInt(reader, "HeartbeatFreshDeviceCount");
        }

        var pct = enrolledCount == 0 ? 0 : Math.Round(100.0 * freshCount / enrolledCount, 1);

        return new DlpEndpointAgentStatusDto(
            EnrolledDeviceCount: enrolledCount,
            HeartbeatFreshDeviceCount: freshCount,
            AgentHeartbeatPct: pct);
    }

    // Pure in-memory scoring over the two risk result sets already read above — unchanged from the
    // pre-batching logic, just no longer interleaved with the SQL reads themselves.
    private static (IReadOnlyList<DlpRiskUserDto> TopRiskyUsers, int HighRiskUserCount, IReadOnlyList<DlpDepartmentRiskDto> RiskByDepartment) ComputeRiskScores(
        List<(Guid EmployeeId, string DisplayName, string? DepartmentName, long BlockCount)> employeeRows,
        Dictionary<Guid, List<DlpRiskFactorDto>> factorsByEmployee)
    {
        var maxBlockCount = employeeRows.Count == 0 ? 0 : employeeRows.Max(x => x.BlockCount);

        var scored = employeeRows.Select(x =>
        {
            var score = maxBlockCount == 0 ? 0 : (int)Math.Round(100.0 * x.BlockCount / maxBlockCount);
            var level = score >= RiskHighThreshold ? "High" : score >= RiskMediumThreshold ? "Medium" : "Low";
            var topFactors = factorsByEmployee.TryGetValue(x.EmployeeId, out var factors)
                ? factors.OrderByDescending(f => f.Count).Take(3).ToList()
                : new List<DlpRiskFactorDto>();

            return new
            {
                x.EmployeeId,
                x.DisplayName,
                x.DepartmentName,
                Score = score,
                Level = level,
                TopFactors = topFactors
            };
        }).ToList();

        var topRiskyUsers = scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.DisplayName)
            .Take(TopRiskyUserCount)
            .Select(x => new DlpRiskUserDto(x.EmployeeId, x.DisplayName, x.DepartmentName, x.Score, x.Level, x.TopFactors))
            .ToList();

        var highRiskUserCount = scored.Count(x => x.Level == "High");

        var riskByDepartment = scored
            .GroupBy(x => x.DepartmentName ?? "Unassigned")
            .Select(g => new DlpDepartmentRiskDto(g.Key, (int)Math.Round(g.Average(x => x.Score))))
            .OrderByDescending(x => x.RiskScore)
            .ToList();

        return (topRiskyUsers, highRiskUserCount, riskByDepartment);
    }

    private static string? BuildDetails(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Truncate(metadataJson, 300);
            }

            var root = document.RootElement;
            var preferredKeys = new[]
            {
                "fileName",
                "filePath",
                "path",
                "processName",
                "processPath",
                "url",
                "targetUrl",
                "website",
                "domain",
                "installerName",
                "executablePath",
                "deviceName",
                "deviceClass",
                "vendorId",
                "productId",
                "windowTitle",
                "applicationName",
                "method"
            };

            var parts = new List<string>();

            foreach (var key in preferredKeys)
            {
                if (TryGetPropertyIgnoreCase(root, key, out var value))
                {
                    var displayValue = ToDisplayValue(value);
                    if (!string.IsNullOrWhiteSpace(displayValue))
                    {
                        parts.Add($"{ToLabel(key)}: {displayValue}");
                    }
                }

                if (parts.Count >= 3)
                {
                    break;
                }
            }

            if (parts.Count > 0)
            {
                return Truncate(string.Join(" | ", parts), 350);
            }

            foreach (var property in root.EnumerateObject())
            {
                var displayValue = ToDisplayValue(property.Value);
                if (!string.IsNullOrWhiteSpace(displayValue))
                {
                    parts.Add($"{ToLabel(property.Name)}: {displayValue}");
                }

                if (parts.Count >= 4)
                {
                    break;
                }
            }

            return parts.Count > 0
                ? Truncate(string.Join(" | ", parts), 350)
                : Truncate(metadataJson, 300);
        }
        catch
        {
            return Truncate(metadataJson, 300);
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string name, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? ToDisplayValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Object => "[object]",
            JsonValueKind.Array => "[array]",
            _ => null
        };
    }

    private static string ToLabel(string key)
    {
        return key switch
        {
            "fileName" => "File",
            "filePath" => "Path",
            "processName" => "Process",
            "processPath" => "Process Path",
            "targetUrl" => "URL",
            "installerName" => "Installer",
            "executablePath" => "Executable",
            "deviceName" => "Device",
            "deviceClass" => "Device Class",
            "vendorId" => "Vendor",
            "productId" => "Product",
            "windowTitle" => "Window",
            "applicationName" => "App",
            _ => key
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...";
    }

    private static string GetString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private static string? GetNullableString(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static long GetLong(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static long? GetNullableLong(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static int GetInt(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static DateTimeOffset GetDateTimeOffset(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(SqlDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }
}
