using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentHeartbeat;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentHeartbeatService
    {
        Task<ApiResponse<AgentHeartbeatResultDto>> ReceiveHeartbeatAsync(
           string deviceKey,
           string agentSecret,
           string? agentVersion,
           AgentHeartbeatRequestDto? request,
           CancellationToken cancellationToken = default);
    }
}
