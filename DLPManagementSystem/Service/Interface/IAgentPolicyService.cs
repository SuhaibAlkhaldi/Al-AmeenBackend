using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentPolicy;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentPolicyService
    {
        Task<ApiResponse<AgentPolicyResultDto>> GetPolicyAsync(
            string deviceKey,
            string agentSecret,
            Guid tenantId,
            Guid deviceId,
            long currentVersion,
            CancellationToken cancellationToken = default);
    }
}
