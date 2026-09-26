using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFileInventory;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAgentFileInventoryService
    {
        Task<ApiResponse<AgentFileInventoryBatchResultDto>> ApplyFileInventoryBatchAsync(
            Guid organizationId,
            Guid deviceId,
            AgentFileInventoryBatchRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
