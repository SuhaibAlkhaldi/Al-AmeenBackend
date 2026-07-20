using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Alerts;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAlertService
    {
        Task<ApiResponse<PagedResultDto<AlertListItemDto>>> GetAlertsAsync(
            Guid organizationId,
            int? statusId,
            int? levelId,
            Guid? assignedToUserId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<AlertDetailDto>> GetAlertByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

        Task<ApiResponse<AlertListItemDto>> AssignAlertAsync(Guid organizationId, Guid id, AssignAlertDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<AlertListItemDto>> UpdateAlertStatusAsync(Guid organizationId, Guid id, UpdateAlertStatusDto request, CancellationToken cancellationToken = default);
    }
}
