namespace DLPManagementSystem.DTO.AgentFiles
{
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
