using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentAuditEvents;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentAuditEventService
    {
        Task<ApiResponse<AgentAuditBatchResultDto>> ReceiveAuditEventBatchAsync(
            Guid organizationId,
            Guid deviceId,
            AgentAuditBatchRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
