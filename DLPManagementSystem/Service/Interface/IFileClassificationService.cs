using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;

namespace DLPManagementSystem.Service.Interface
{
    public interface IFileClassificationService
    {
        // fileContent is the actual file bytes for a background-scan classification (see
        // CompanyDlp.Service's FileInventoryScanner, which is the only agent-side caller that has real
        // file access and supplies this). Null for any other caller - real, content-based AI
        // classification requires it, so a null stream falls back to the extension-blocklist stub
        // rather than fabricating a content-based verdict without ever reading the content.
        Task<ApiResponse<FileClassificationResultDto>> ClassifyAsync(
            Guid organizationId,
            Guid deviceId,
            FileClassificationRequestDto request,
            Stream? fileContent,
            CancellationToken cancellationToken = default);
    }
}
