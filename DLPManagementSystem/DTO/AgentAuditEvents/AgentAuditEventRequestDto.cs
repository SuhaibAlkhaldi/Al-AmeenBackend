using System.Text.Json;

namespace DLPManagementSystem.DTO.AgentAuditEvents
{
    public class AgentAuditEventRequestDto
    {
        public Guid CorrelationId { get; set; }

        public string UserSid { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string ActionKey { get; set; } = string.Empty;

        public string Decision { get; set; } = string.Empty;

        public string ReasonCode { get; set; } = string.Empty;

        public string? EventType { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public JsonElement? Metadata { get; set; }
    }
}
