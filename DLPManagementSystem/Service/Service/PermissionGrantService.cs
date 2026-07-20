using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class PermissionGrantService : IPermissionGrantService
    {
        private readonly DLPSystemContext _db;

        public PermissionGrantService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<PagedResultDto<PermissionGrantDto>>> GetGrantsAsync(
            Guid organizationId,
            string? subjectId,
            string? actionKey,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.PermissionGrants
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (!string.IsNullOrWhiteSpace(subjectId))
            {
                query = query.Where(x => x.SubjectId == subjectId);
            }

            if (!string.IsNullOrWhiteSpace(actionKey))
            {
                query = query.Where(x => x.ActionKey == actionKey);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .Include(x => x.Decision)
                .Include(x => x.SubjectType)
                .Include(x => x.GrantType)
                .Include(x => x.GrantedByUser)
                .Include(x => x.TargetDevice)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var nowUtc = DateTimeOffset.UtcNow;
            var items = entities.Select(x => MapToDto(x, nowUtc)).ToList();

            var result = new PagedResultDto<PermissionGrantDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<PermissionGrantDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<bool>> RevokeAsync(
            Guid organizationId,
            Guid id,
            Guid revokedByUserId,
            RevokePermissionGrantDto request,
            CancellationToken cancellationToken = default)
        {
            var grant = await _db.PermissionGrants
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (grant == null)
            {
                return ApiResponse<bool>.FailureResponse("Permission grant was not found.", "منحة الصلاحية غير موجودة");
            }

            if (grant.RevokedAtUtc != null)
            {
                return ApiResponse<bool>.FailureResponse("Permission grant is already revoked.", "تم إلغاء منحة الصلاحية مسبقًا");
            }

            grant.RevokedAtUtc = DateTimeOffset.UtcNow;
            grant.RevokedByUserId = revokedByUserId;
            grant.RevocationReason = request.RevocationReason;

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Permission grant revoked successfully.", "تم إلغاء منحة الصلاحية بنجاح");
        }

        private static PermissionGrantDto MapToDto(PermissionGrant grant, DateTimeOffset nowUtc)
        {
            string runtimeStatus;

            if (grant.RevokedAtUtc != null)
            {
                runtimeStatus = "Revoked";
            }
            else if (grant.ExpiresAtUtc != null && grant.ExpiresAtUtc <= nowUtc)
            {
                runtimeStatus = "Expired";
            }
            else if (grant.StartsAtUtc > nowUtc)
            {
                runtimeStatus = "Pending";
            }
            else
            {
                runtimeStatus = "Active";
            }

            return new PermissionGrantDto
            {
                Id = grant.Id,
                OrganizationId = grant.OrganizationId,
                ActionKey = grant.ActionKey,
                Decision = grant.Decision.Name,
                SubjectType = grant.SubjectType.Name,
                SubjectId = grant.SubjectId,
                TargetDeviceId = grant.TargetDeviceId,
                TargetDeviceName = grant.TargetDevice?.MachineName,
                GrantType = grant.GrantType.Name,
                Priority = grant.Priority,
                StartsAtUtc = grant.StartsAtUtc,
                ExpiresAtUtc = grant.ExpiresAtUtc,
                RuntimeStatus = runtimeStatus,
                Reason = grant.Reason,
                GrantedByUserId = grant.GrantedByUserId,
                GrantedByUserName = grant.GrantedByUser.FullName,
                SourcePermissionRequestId = grant.SourcePermissionRequestId,
                CreatedAtUtc = grant.CreatedAtUtc,
                RevokedAtUtc = grant.RevokedAtUtc,
                RevocationReason = grant.RevocationReason
            };
        }
    }
}
