namespace DLPManagementSystem.DTO.AgentFiles
{
    // Mirrors CompanyDlp.Contracts.ClassificationTiers on the agent side - string values must stay
    // identical since grants/requests carry them as plain strings across the backend/agent boundary,
    // and they must match the AI classifier API's `classification` response values verbatim.
    public static class ClassificationTiers
    {
        public const string Public = "Public";
        public const string Internal = "Internal";
        public const string Secret = "Secret";
        public const string VerySecret = "Very_Secret";

        public static readonly IReadOnlyList<string> Order = [Public, Internal, Secret, VerySecret];

        public static bool IsValidRequestableTier(string? tier) =>
            tier is Internal or Secret or VerySecret;
    }

    public class FileClassificationRequestDto
    {
        public Guid RequestId { get; set; }
        public Guid TenantId { get; set; }
        public Guid DeviceId { get; set; }
        public string? UserSid { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? Extension { get; set; }
        public long SizeBytes { get; set; }
        public string? MimeType { get; set; }
        public string? Sha256 { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTimeOffset RequestedAtUtc { get; set; }
    }

    public class FileClassificationResultDto
    {
        public Guid RequestId { get; set; }
        public bool IsAllowed { get; set; }
        public bool IsSensitive { get; set; }
        public string Classification { get; set; } = string.Empty;
        public string ReasonCode { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? RuleId { get; set; }
        public DateTimeOffset EvaluatedAtUtc { get; set; }
        public DateTimeOffset? ValidUntilUtc { get; set; }
    }
}
