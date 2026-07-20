using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Permissions.Contracts;

namespace DLPManagementSystem.Service.Interface
{
    public interface IPermissionGrantService
    {
        Task<ApiResponse<PagedResultDto<PermissionGrantDto>>> GetGrantsAsync(
            Guid organizationId,
            string? subjectId,
            string? actionKey,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> RevokeAsync(
            Guid organizationId,
            Guid id,
            Guid revokedByUserId,
            RevokePermissionGrantDto request,
            CancellationToken cancellationToken = default);
    }
}
