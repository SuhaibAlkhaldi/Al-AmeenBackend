using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AuditEvents;
using Microsoft.AspNetCore.Http;

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

        // Streams every row matching the filters (no paging) as text/csv directly into the response body.
        Task ExportAuditEventsAsync(
            Guid organizationId,
            Guid? deviceId,
            Guid? employeeId,
            string? actionKey,
            int? decisionId,
            int? reasonCodeId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            HttpResponse response,
            CancellationToken cancellationToken = default);
    }
}
