using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Permissions.Contracts;

namespace DLPManagementSystem.Service.Interface
{
    public interface IPermissionRequestService
    {
        Task<ApiResponse<PagedResultDto<PermissionRequestDto>>> GetRequestsAsync(
            Guid organizationId,
            Guid callerUserId,
            int callerUserTypeId,
            int? statusId,
            Guid? requestedByEmployeeId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Count of requests still awaiting a decision (Submitted/UnderReview) — org-wide for Admin-type
        /// callers, scoped to the caller's own requests for Employee-type callers (same scoping rule as
        /// <see cref="GetRequestsAsync"/>). Cheap by design: no paging, no DTO projection, just a count.
        /// </summary>
        Task<ApiResponse<int>> GetPendingCountAsync(
            Guid organizationId,
            Guid callerUserId,
            int callerUserTypeId,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PermissionRequestDto>> GetByIdAsync(
            Guid organizationId,
            Guid id,
            Guid callerUserId,
            int callerUserTypeId,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PermissionRequestDto>> CreateAsync(
            Guid organizationId,
            Guid requestedByUserId,
            int callerUserTypeId,
            CreatePermissionRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PermissionRequestDto>> ApproveAsync(
            Guid organizationId,
            Guid id,
            Guid reviewedByUserId,
            int callerUserTypeId,
            ReviewPermissionRequestDto request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PermissionRequestDto>> RejectAsync(
            Guid organizationId,
            Guid id,
            Guid reviewedByUserId,
            int callerUserTypeId,
            ReviewPermissionRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
