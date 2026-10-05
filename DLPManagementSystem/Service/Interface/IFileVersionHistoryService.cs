using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileChainOfCustody;

namespace DLPManagementSystem.Service.Interface
{
    public interface IFileVersionHistoryService
    {
        Task<ApiResponse<FileVersionHistoryResultDto>> GetHistoryAsync(
            Guid organizationId,
            Guid fileId,
            CancellationToken cancellationToken = default);
    }
}
