using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;

namespace DLPManagementSystem.Service.Interface
{
    public interface IFileClassificationService
    {
        Task<ApiResponse<FileClassificationResultDto>> ClassifyAsync(
            Guid organizationId,
            Guid deviceId,
            FileClassificationRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
