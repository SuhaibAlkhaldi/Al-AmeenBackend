namespace DLPManagementSystem.Common
{
    // Minimal RFC 4180 CSV field/row writer. No new NuGet dependency for a job this narrow — the escaping
    // rule is exactly "quote the field and double any embedded quotes whenever it contains a comma, quote,
    // or newline", which this implements directly rather than pulling in CsvHelper for one method.
    public static class CsvWriterHelper
    {
        public static string EscapeField(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var needsQuoting = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;

            if (!needsQuoting)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static string BuildRow(params string?[] fields)
        {
            return string.Join(",", fields.Select(EscapeField));
        }
    }
}
