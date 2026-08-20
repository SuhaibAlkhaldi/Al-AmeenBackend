using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AdminAuditLogs;
using DLPManagementSystem.Service.Interface;

namespace DLPManagementSystem.Tests.TestHelpers
{
    // No-op stand-in for the audit log write path - the services under test call LogAsync as a
    // side effect, but none of these tests assert on audit log contents.
    public sealed class FakeAdminAuditLogService : IAdminAuditLogService
    {
        public Task LogAsync(
            Guid organizationId,
            Guid actorUserId,
            string actionType,
            string targetType,
            Guid? targetId,
            string? targetDisplayName,
            string? details,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ApiResponse<PagedResultDto<AdminAuditLogListItemDto>>> GetAdminAuditLogsAsync(
            Guid organizationId,
            Guid? actorUserId,
            string? actionType,
            string? targetType,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");
    }
}
