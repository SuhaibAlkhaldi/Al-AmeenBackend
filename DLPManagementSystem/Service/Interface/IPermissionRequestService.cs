using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Permissions.Contracts;

namespace DLPManagementSystem.Service.Interface
{
    public interface IPermissionRequestService
    {
        Task<ApiResponse<PagedResultDto<PermissionRequestDto>>> GetRequestsAsync(
            Guid organizationId,
            int? statusId,
            Guid? requestedByEmployeeId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PermissionRequestDto>> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

        Task<ApiResponse<PermissionRequestDto>> CreateAsync(
            Guid organizationId,
            Guid requestedByUserId,
            CreatePermissionRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PermissionRequestDto>> ApproveAsync(
            Guid organizationId,
            Guid id,
            Guid reviewedByUserId,
            ReviewPermissionRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PermissionRequestDto>> RejectAsync(
            Guid organizationId,
            Guid id,
            Guid reviewedByUserId,
            ReviewPermissionRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
