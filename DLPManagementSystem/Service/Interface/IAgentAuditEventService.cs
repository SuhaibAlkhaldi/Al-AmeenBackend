using System.Text.Json;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentAuditEvents;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentAuditEventService
    {
        // rawBody is the same request body the controller bound `request` from - needed to verify each
        // event's integrityHash against its original wire bytes rather than the re-typed DTO. See
        // AuditIntegrityVerifier for why.
        Task<ApiResponse<AgentAuditBatchResultDto>> ReceiveAuditEventBatchAsync(
            Guid organizationId,
            Guid deviceId,
            AgentAuditBatchRequestDto request,
            JsonElement rawBody,
            CancellationToken cancellationToken = default);
    }
}
