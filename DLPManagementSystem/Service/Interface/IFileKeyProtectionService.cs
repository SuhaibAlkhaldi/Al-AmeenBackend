using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;

namespace DLPManagementSystem.Service.Interface
{
    public interface IFileKeyProtectionService
    {
        ApiResponse<FileKeyWrapResponseDto> Wrap(FileKeyWrapRequestDto request);

        ApiResponse<FileKeyUnwrapResponseDto> Unwrap(FileKeyUnwrapRequestDto request);
    }
}
