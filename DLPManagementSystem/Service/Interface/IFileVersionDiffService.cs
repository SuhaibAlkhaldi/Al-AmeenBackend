using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileTracking;

namespace DLPManagementSystem.Service.Interface
{
    public interface IFileVersionDiffService
    {
        // Returns the diff for one recorded version against the version before it. A version that does not exist in
        // the caller's organization fails with a not-found response. Every other outcome, including "text not
        // available", is a success whose DTO says why no diff is shown.
        Task<ApiResponse<FileVersionDiffDto>> GetDiffAsync(Guid organizationId, Guid userId, Guid versionId, CancellationToken cancellationToken);
    }
}
