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
        private readonly IPolicyVersionService _policyVersionService;

        public PermissionGrantService(DLPSystemContext db, IPolicyVersionService policyVersionService)
        {
            _db = db;
            _policyVersionService = policyVersionService;
        }

        public async Task<ApiResponse<PagedResultDto<PermissionGrantDto>>> GetGrantsAsync(
            Guid organizationId,
            string? subjectId,
            string? actionKey,
            string? status,
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

            query = query
                .Include(x => x.Decision)
                .Include(x => x.SubjectType)
                .Include(x => x.GrantType)
                .Include(x => x.GrantedByUser)
                .Include(x => x.RevokedByUser)
                .Include(x => x.TargetDevice)
                .OrderByDescending(x => x.CreatedAtUtc);

            var nowUtc = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(status))
            {
                // Same Active/Pending/Expired/Revoked classification PermissionGrantRuntimeStatus.Compute
                // uses in-memory, translated to SQL so status filtering pages at the database level
                // instead of materializing every matching grant first.
                query = query.Where(PermissionGrantRuntimeStatus.MatchesStatus(status, nowUtc));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var pageEntities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var employeeSubjectIds = pageEntities
                .Where(x => x.SubjectType.Name == "Employee" && Guid.TryParse(x.SubjectId, out _))
                .Select(x => Guid.Parse(x.SubjectId))
                .Distinct()
                .ToList();

            var employeeNames = employeeSubjectIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Employees
                    .AsNoTracking()
                    .Where(x => x.OrganizationId == organizationId && employeeSubjectIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.DisplayName })
                    .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

            var items = pageEntities.Select(x => MapToDto(x, nowUtc, employeeNames)).ToList();

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

            var nowUtc = DateTimeOffset.UtcNow;
            ApplyRevocation(grant, revokedByUserId, request.RevocationReason, nowUtc);

            await _policyVersionService.BumpAsync(
                organizationId,
                revokedByUserId,
                "GrantRevoked",
                "PermissionGrant",
                grant.Id,
                $"Permission '{grant.ActionKey}' revoked for subject {grant.SubjectId}.",
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Permission grant revoked successfully.", "تم إلغاء منحة الصلاحية بنجاح");
        }

        public async Task<ApiResponse<RevokeAllGrantsResultDto>> RevokeAllAsync(
            Guid organizationId,
            Guid revokedByUserId,
            RevokeAllGrantsDto request,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTimeOffset.UtcNow;

            var candidates = await _db.PermissionGrants
                .Where(x => x.OrganizationId == organizationId && x.SubjectId == request.SubjectId && x.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            var toRevoke = candidates
                .Where(x => PermissionGrantRuntimeStatus.Compute(x.RevokedAtUtc, x.ExpiresAtUtc, x.StartsAtUtc, nowUtc) is "Active" or "Pending")
                .ToList();

            if (toRevoke.Count == 0)
            {
                return ApiResponse<RevokeAllGrantsResultDto>.FailureResponse(
                    "No active or pending permission grants were found for this subject.",
                    "لا توجد منح صلاحيات نشطة أو معلقة لهذا الموضوع");
            }

            foreach (var grant in toRevoke)
            {
                ApplyRevocation(grant, revokedByUserId, request.RevocationReason, nowUtc);
            }

            await _policyVersionService.BumpAsync(
                organizationId,
                revokedByUserId,
                "GrantsBulkRevoked",
                "PermissionGrant",
                null,
                $"{toRevoke.Count} permission grant(s) revoked for subject {request.SubjectId}.",
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            var result = new RevokeAllGrantsResultDto
            {
                RevokedCount = toRevoke.Count,
                ActionKeys = toRevoke.Select(x => x.ActionKey).ToList()
            };

            return ApiResponse<RevokeAllGrantsResultDto>.SuccessResponse(
                result,
                $"{toRevoke.Count} permission grant(s) revoked successfully.",
                $"تم إلغاء {toRevoke.Count} منحة صلاحية بنجاح");
        }

        private static void ApplyRevocation(PermissionGrant grant, Guid revokedByUserId, string reason, DateTimeOffset nowUtc)
        {
            grant.RevokedAtUtc = nowUtc;
            grant.RevokedByUserId = revokedByUserId;
            grant.RevocationReason = reason;
        }

        private static string ComputeRuntimeStatus(PermissionGrant grant, DateTimeOffset nowUtc)
        {
            return PermissionGrantRuntimeStatus.Compute(grant.RevokedAtUtc, grant.ExpiresAtUtc, grant.StartsAtUtc, nowUtc);
        }

        private static PermissionGrantDto MapToDto(PermissionGrant grant, DateTimeOffset nowUtc, IReadOnlyDictionary<Guid, string> employeeNames)
        {
            var runtimeStatus = ComputeRuntimeStatus(grant, nowUtc);

            string? subjectDisplayName = null;
            if (grant.SubjectType.Name == "Employee" && Guid.TryParse(grant.SubjectId, out var subjectGuid))
            {
                employeeNames.TryGetValue(subjectGuid, out subjectDisplayName);
            }

            return new PermissionGrantDto
            {
                Id = grant.Id,
                OrganizationId = grant.OrganizationId,
                ActionKey = grant.ActionKey,
                Decision = grant.Decision.Name,
                SubjectType = grant.SubjectType.Name,
                SubjectId = grant.SubjectId,
                SubjectDisplayName = subjectDisplayName,
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
                RevocationReason = grant.RevocationReason,
                RevokedByUserName = grant.RevokedByUser?.FullName
            };
        }
    }
}
