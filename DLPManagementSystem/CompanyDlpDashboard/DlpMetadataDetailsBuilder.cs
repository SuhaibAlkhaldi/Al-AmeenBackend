using System.Text.Json;

namespace DLPManagementSystem.CompanyDlpDashboard;

public static class DlpMetadataDetailsBuilder
{
    public static string? Build(string? metadataJson)
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
                return Truncate(metadataJson, 350);
            }

            var root = document.RootElement;

            var originalEventType = GetPath(root, "originalEventType");
            var machineName = GetPath(root, "machineName");

            var sourceProcessName = GetPath(root, "sourceProcess.name");
            var sourceProcessPath = GetPath(root, "sourceProcess.path");
            var sourceProcessId = GetPath(root, "sourceProcess.processId");

            var destination = GetPath(root, "destination.value");

            var resourceName = GetPath(root, "resource.name");
            var resourceType = GetPath(root, "resource.type");
            var resourceExtension = GetPath(root, "resource.extension");
            var maskedPath = GetPath(root, "resource.maskedPath");

            var action = GetPath(root, "details.action");
            var method = GetPath(root, "details.method");
            var details = GetPath(root, "details.details");

            var callerProcessName = GetPath(root, "details.ipcClient.callerProcessName");
            var callerProcessPath = GetPath(root, "details.ipcClient.callerProcessPath");
            var declaredName = GetPath(root, "details.ipcClient.declaredName");

            var parts = new List<string>();

            Add(parts, "Event", originalEventType);
            Add(parts, "Machine", machineName);
            Add(parts, "Destination", destination);
            Add(parts, "Resource", BuildResource(resourceType, resourceName, resourceExtension, maskedPath));
            Add(parts, "Action", action);
            Add(parts, "Method", method);
            Add(parts, "Process", BuildProcess(sourceProcessName, sourceProcessId));
            Add(parts, "Caller", FirstNonEmpty(callerProcessName, declaredName));

            var compactDetails = CleanupDetails(details);
            Add(parts, "Details", compactDetails);

            if (parts.Count == 0)
            {
                return Truncate(metadataJson, 350);
            }

            return Truncate(string.Join(" | ", parts), 600);
        }
        catch
        {
            return Truncate(metadataJson, 350);
        }
    }

    private static string? GetPath(JsonElement root, string path)
    {
        var current = root;

        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var found = false;

            foreach (var property in current.EnumerateObject())
            {
                if (string.Equals(property.Name, segment, StringComparison.OrdinalIgnoreCase))
                {
                    current = property.Value;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return null;
            }
        }

        return ToDisplayValue(current);
    }

    private static string? ToDisplayValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => EmptyToNull(value.GetString()),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string? BuildProcess(string? name, string? processId)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(processId))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(processId))
        {
            return $"{name} (PID {processId})";
        }

        return FirstNonEmpty(name, processId);
    }

    private static string? BuildResource(string? type, string? name, string? extension, string? maskedPath)
    {
        if (!string.IsNullOrWhiteSpace(maskedPath))
        {
            return maskedPath;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(extension))
        {
            return $"{type} {extension}";
        }

        return FirstNonEmpty(type, extension);
    }

    private static string? CleanupDetails(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value
            .Replace("\\u0022", "\"")
            .Replace("\\u0026", "&")
            .Trim();

        if (cleaned.StartsWith("silent-background:", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring("silent-background:".Length);
        }

        return Truncate(cleaned, 180);
    }

    private static void Add(List<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value}");
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...";
    }
}
