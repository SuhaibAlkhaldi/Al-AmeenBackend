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

            int totalCount;
            List<PermissionGrant> pageEntities;

            if (!string.IsNullOrWhiteSpace(status))
            {
                // RuntimeStatus is computed at read time rather than stored, so filtering by it
                // requires materializing all matching grants first. Org grant volume is small
                // enough (low thousands at most) for this to be cheap.
                var allEntities = await query.ToListAsync(cancellationToken);
                var filtered = allEntities
                    .Where(x => string.Equals(ComputeRuntimeStatus(x, nowUtc), status, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                totalCount = filtered.Count;
                pageEntities = filtered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }
            else
            {
                totalCount = await query.CountAsync(cancellationToken);
                pageEntities = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
            }

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

            grant.RevokedAtUtc = DateTimeOffset.UtcNow;
            grant.RevokedByUserId = revokedByUserId;
            grant.RevocationReason = request.RevocationReason;

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
