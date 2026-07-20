using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentAuditEvents;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentAuditEventService
    {
        Task<ApiResponse<AgentAuditBatchResultDto>> ReceiveAuditEvents(string deviceKey,string agentSecret,AgentAuditBatchRequestDto request,CancellationToken cancellationToken = default);
    }
}
