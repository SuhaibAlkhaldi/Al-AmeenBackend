using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AuditEvents;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAuditEventService
    {
        Task<ApiResponse<PagedResultDto<AuditEventListItemDto>>> GetAuditEventsAsync(
            Guid organizationId,
            Guid? deviceId,
            Guid? employeeId,
            string? actionKey,
            int? decisionId,
            int? reasonCodeId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
