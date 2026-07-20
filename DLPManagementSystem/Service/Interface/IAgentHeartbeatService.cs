using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentHeartbeat;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentHeartbeatService
    {
        Task<ApiResponse<AgentHeartbeatResultDto>> ReceiveHeartbeatAsync(
           Guid organizationId,
           Guid deviceId,
           AgentHeartbeatRequestDto request,
           CancellationToken cancellationToken = default);
    }
}
