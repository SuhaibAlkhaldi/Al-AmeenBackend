using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AdminAuditLogs;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class AdminAuditLogService : IAdminAuditLogService
    {
        private readonly DLPSystemContext _db;

        public AdminAuditLogService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task LogAsync(
            Guid organizationId,
            Guid actorUserId,
            string actionType,
            string targetType,
            Guid? targetId,
            string? targetDisplayName,
            string? details,
            CancellationToken cancellationToken = default)
        {
            // Looked up here (rather than threaded through every caller's signature) so every call site
            // only needs to pass the actor's id - the denormalized snapshot fields are captured from
            // whatever the actor's email/name/role are right now, at write time.
            var actor = await _db.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == actorUserId, cancellationToken);

            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AdminUserId = actorUserId,
                ActorEmail = actor?.Email ?? string.Empty,
                ActorFullName = actor?.FullName ?? string.Empty,
                ActorRoleName = actor?.Role?.Name ?? string.Empty,
                Action = actionType,
                EntityType = targetType,
                EntityId = targetId,
                TargetDisplayName = targetDisplayName,
                MetadataJson = details,
                OccurredAtUtc = DateTimeOffset.UtcNow
            });
        }

        public async Task<ApiResponse<PagedResultDto<AdminAuditLogListItemDto>>> GetAdminAuditLogsAsync(
            Guid organizationId,
            Guid? actorUserId,
            string? actionType,
            string? targetType,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.AdminAuditLogs
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (actorUserId.HasValue)
            {
                query = query.Where(x => x.AdminUserId == actorUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(x => x.Action == actionType);
            }

            if (!string.IsNullOrWhiteSpace(targetType))
            {
                query = query.Where(x => x.EntityType == targetType);
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAtUtc <= toUtc.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.OccurredAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminAuditLogListItemDto
                {
                    Id = x.Id,
                    OccurredAtUtc = x.OccurredAtUtc,
                    ActorUserId = x.AdminUserId,
                    ActorEmail = x.ActorEmail,
                    ActorFullName = x.ActorFullName,
                    ActorRoleName = x.ActorRoleName,
                    ActionType = x.Action,
                    TargetType = x.EntityType,
                    TargetId = x.EntityId,
                    TargetDisplayName = x.TargetDisplayName,
                    Details = x.MetadataJson
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<AdminAuditLogListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<AdminAuditLogListItemDto>>.SuccessResponse(result);
        }
    }
}
