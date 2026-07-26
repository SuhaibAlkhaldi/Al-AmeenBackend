namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    // Prefill data for the exact-file request path: the portal calls GET
    // /api/v1/permission-requests/source-event/{correlationId} with a CorrelationId it read off a
    // "Request Permission" deep link (from a blocked-attempt notification), and shows these details
    // read-only before the employee submits - the employee never types in file hash/classification
    // themselves, since that's server-derived from the agent-reported AuditEvent.
    public sealed class SourceEventDetailsDto
    {
        public Guid CorrelationId { get; set; }
        public string ActionKey { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = string.Empty;
        public string? Classification { get; set; }
        public string? ClassificationReasonCode { get; set; }
        public DateTimeOffset OccurredAtUtc { get; set; }
    }
}
