using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

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

    public async Task<DlpDashboardSummaryDto> GetSummaryAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var to = (toUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
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

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var totals = await GetTotalsAsync(connection, from, to, cancellationToken);
        var actionBreakdown = await GetActionBreakdownAsync(connection, from, to, cancellationToken);
        var reasonBreakdown = await GetReasonBreakdownAsync(connection, from, to, cancellationToken);
        var recentEvents = await GetRecentEventsAsync(connection, from, to, cancellationToken);

        return new DlpDashboardSummaryDto(
            DateTimeOffset.UtcNow,
            from,
            to,
            totals.PolicyVersion,
            totals.Totals,
            actionBreakdown,
            reasonBreakdown,
            recentEvents);
    }

    private static async Task<(DlpDashboardTotalsDto Totals, long? PolicyVersion)> GetTotalsAsync(
        SqlConnection connection,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        const string sql = @"
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
WHERE ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc;";

        await using var command = CreateCommand(connection, sql, from, to);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);

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

    private static async Task<IReadOnlyList<DlpActionBreakdownItemDto>> GetActionBreakdownAsync(
        SqlConnection connection,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TOP (30)
    ae.ActionKey,
    COUNT_BIG(1) AS Total,
    SUM(CASE WHEN ad.Name = N'Allow' THEN 1 ELSE 0 END) AS Allowed,
    SUM(CASE WHEN ad.Name = N'Block' THEN 1 ELSE 0 END) AS Blocked,
    SUM(CASE WHEN ad.Name = N'Error' THEN 1 ELSE 0 END) AS Errors,
    MAX(ae.ReceivedAtUtc) AS LastReceivedAtUtc
FROM dbo.AuditEvents ae
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
WHERE ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc
GROUP BY ae.ActionKey
ORDER BY COUNT_BIG(1) DESC, ae.ActionKey ASC;";

        var result = new List<DlpActionBreakdownItemDto>();

        await using var command = CreateCommand(connection, sql, from, to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

    private static async Task<IReadOnlyList<DlpReasonBreakdownItemDto>> GetReasonBreakdownAsync(
        SqlConnection connection,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TOP (15)
    COALESCE(arc.Code, N'Unknown') AS ReasonCode,
    COUNT_BIG(1) AS Total
FROM dbo.AuditEvents ae
LEFT JOIN dbo.AuditReasonCodes arc ON arc.Id = ae.ReasonCodeId
WHERE ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc
GROUP BY COALESCE(arc.Code, N'Unknown')
ORDER BY COUNT_BIG(1) DESC;";

        var result = new List<DlpReasonBreakdownItemDto>();

        await using var command = CreateCommand(connection, sql, from, to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DlpReasonBreakdownItemDto(
                GetString(reader, "ReasonCode"),
                GetLong(reader, "Total")));
        }

        return result;
    }

    private static async Task<IReadOnlyList<DlpRecentAuditEventDto>> GetRecentEventsAsync(
        SqlConnection connection,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TOP (5)
    ae.ActionKey,
    CAST(NULL AS nvarchar(200)) AS EventType,
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
LEFT JOIN dbo.AuditDecisions ad ON ad.Id = ae.DecisionId
LEFT JOIN dbo.AuditReasonCodes arc ON arc.Id = ae.ReasonCodeId
WHERE ae.ReceivedAtUtc >= @FromUtc
  AND ae.ReceivedAtUtc < @ToUtc
ORDER BY ae.ReceivedAtUtc DESC, ae.OccurredAtUtc DESC;";

        var result = new List<DlpRecentAuditEventDto>();

        await using var command = CreateCommand(connection, sql, from, to);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

    private static SqlCommand CreateCommand(SqlConnection connection, string sql, DateTimeOffset from, DateTimeOffset to)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;
        command.Parameters.Add(new SqlParameter("@FromUtc", SqlDbType.DateTimeOffset) { Value = from });
        command.Parameters.Add(new SqlParameter("@ToUtc", SqlDbType.DateTimeOffset) { Value = to });
        return command;
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

