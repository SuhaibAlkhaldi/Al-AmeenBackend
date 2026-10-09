using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileChainOfCustody;

namespace DLPManagementSystem.Service.Interface
{
    public interface IFileChainOfCustodyService
    {
        Task<ApiResponse<FileChainOfCustodyResultDto>> GetChainOfCustodyAsync(
            Guid organizationId,
            Guid fileId,
            CancellationToken cancellationToken = default);
    }
}
