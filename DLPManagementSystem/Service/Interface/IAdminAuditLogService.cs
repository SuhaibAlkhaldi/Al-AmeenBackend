using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AdminAuditLogs;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAdminAuditLogService
    {
        // Stages a new AdminAuditLog row on the shared DbContext without calling SaveChangesAsync —
        // mirrors IPolicyVersionService.BumpAsync so the log write commits atomically together with
        // whatever admin mutation the caller is already about to save, in the same transaction.
        Task LogAsync(
            Guid organizationId,
            Guid actorUserId,
            string actionType,
            string targetType,
            Guid? targetId,
            string? targetDisplayName,
            string? details,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<PagedResultDto<AdminAuditLogListItemDto>>> GetAdminAuditLogsAsync(
            Guid organizationId,
            Guid? actorUserId,
            string? actionType,
            string? targetType,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
