using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentPolicy;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentPolicyService
    {
        Task<ApiResponse<AgentPolicyResultDto>> GetPolicyAsync(
            Guid organizationId,
            Guid deviceId,
            long currentVersion,
            string? userSid,
            CancellationToken cancellationToken = default);
    }
}
