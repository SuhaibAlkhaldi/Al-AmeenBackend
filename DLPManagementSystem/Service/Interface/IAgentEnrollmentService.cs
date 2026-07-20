using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentEnrollment;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentEnrollmentService
    {
        Task<ApiResponse<AgentEnrollResponseDto>> Enroll(AgentEnrollRequestDto request,CancellationToken cancellationToken = default);
    }
}
