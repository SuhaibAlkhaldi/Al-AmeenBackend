using System.Text.Json;

namespace DLPManagementSystem.DTO.AgentAuditEvents
{
    public class ProcessContextDto
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public int? ProcessId { get; set; }
        public string? Publisher { get; set; }
        public string? Sha256 { get; set; }
        public string? WindowTitle { get; set; }
    }

    public class ResourceContextDto
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? Extension { get; set; }
        public long? SizeBytes { get; set; }
        public string? Sha256 { get; set; }
        public string? MaskedPath { get; set; }
    }

    public class DestinationContextDto
    {
        public string? Type { get; set; }
        public string? Value { get; set; }
    }

    public class SecurityEventEnvelopeDto
    {
        public Guid EventId { get; set; }
        public Guid CorrelationId { get; set; }
        public string ProtocolVersion { get; set; } = string.Empty;
        public string EventSchemaVersion { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid? UserId { get; set; }
        public string? UserSid { get; set; }
        public string? Username { get; set; }
        public string? MachineName { get; set; }
        public int? WindowsSessionId { get; set; }
        public string ActionKey { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string ReasonCode { get; set; } = string.Empty;
        public Guid? PolicyId { get; set; }
        public long? PolicyVersion { get; set; }
        public string? RuleId { get; set; }
        public Guid? PermissionGrantId { get; set; }
        public ProcessContextDto? SourceProcess { get; set; }
        public ResourceContextDto? Resource { get; set; }
        public DestinationContextDto? Destination { get; set; }
        public JsonElement? Details { get; set; }
        public DateTimeOffset OccurredAtUtc { get; set; }
        public string AgentVersion { get; set; } = string.Empty;
        public string? OsVersion { get; set; }
        public bool IsDevelopmentEvent { get; set; }
        public string IntegrityHash { get; set; } = string.Empty;
    }
}
