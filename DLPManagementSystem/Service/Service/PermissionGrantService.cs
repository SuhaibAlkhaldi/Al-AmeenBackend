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
        private readonly IPermissionLookupService _lookupService;

        public PermissionGrantService(DLPSystemContext db, IPolicyVersionService policyVersionService, IPermissionLookupService lookupService)
        {
            _db = db;
            _policyVersionService = policyVersionService;
            _lookupService = lookupService;
        }

        public async Task<ApiResponse<PagedResultDto<PermissionGrantDto>>> GetGrantsAsync(
            Guid organizationId,
            Guid callerUserId,
            int callerUserTypeId,
            string? subjectId,
            string? actionKey,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var effectiveSubjectId = subjectId;

            if (await IsEmployeeUserTypeAsync(callerUserTypeId, cancellationToken))
            {
                // Employee-type callers can only ever see their own grants — force this regardless of
                // whatever subjectId the client passed, mirroring PermissionRequestService.GetRequestsAsync.
                var callerEmployee = await _db.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == callerUserId, cancellationToken);

                if (callerEmployee == null)
                {
                    return ApiResponse<PagedResultDto<PermissionGrantDto>>.SuccessResponse(new PagedResultDto<PermissionGrantDto>
                    {
                        Items = new List<PermissionGrantDto>(),
                        TotalCount = 0,
                        Page = page,
                        PageSize = pageSize
                    });
                }

                effectiveSubjectId = callerEmployee.Id.ToString();
            }

            var query = _db.PermissionGrants
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (!string.IsNullOrWhiteSpace(effectiveSubjectId))
            {
                query = query.Where(x => x.SubjectId == effectiveSubjectId);
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

        public async Task<GrantBuildResult> BuildGrantAsync(
            Guid organizationId,
            string actionKey,
            int decisionId,
            int subjectTypeId,
            string subjectId,
            Guid? targetDeviceId,
            string grantTypeName,
            DateTimeOffset? requestedStartsAtUtc,
            DateTimeOffset? requestedExpiresAtUtc,
            string reason,
            Guid grantedByUserId,
            Guid? sourcePermissionRequestId,
            CancellationToken cancellationToken = default)
        {
            int grantTypeId;

            try
            {
                grantTypeId = await _lookupService.GetPermissionGrantTypeId(grantTypeName, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return GrantBuildResult.Failure("Required reference data was not found.", "بيانات مرجعية مطلوبة غير موجودة");
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var startsAtUtc = requestedStartsAtUtc ?? nowUtc;
            DateTimeOffset? expiresAtUtc;

            if (string.Equals(grantTypeName, "Temporary", StringComparison.OrdinalIgnoreCase))
            {
                expiresAtUtc = requestedExpiresAtUtc;

                if (expiresAtUtc == null || expiresAtUtc <= startsAtUtc)
                {
                    return GrantBuildResult.Failure(
                        "A temporary grant requires an expiry date/time in the future of its start time.",
                        "المنحة المؤقتة تتطلب تاريخ/وقت انتهاء صلاحية بعد وقت البدء");
                }
            }
            else
            {
                expiresAtUtc = null;
            }

            var grant = new PermissionGrant
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ActionKey = actionKey,
                DecisionId = decisionId,
                SubjectTypeId = subjectTypeId,
                SubjectId = subjectId,
                TargetDeviceId = targetDeviceId,
                GrantTypeId = grantTypeId,
                Priority = 500,
                StartsAtUtc = startsAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                Reason = reason,
                GrantedByUserId = grantedByUserId,
                SourcePermissionRequestId = sourcePermissionRequestId,
                CreatedAtUtc = nowUtc
            };

            _db.PermissionGrants.Add(grant);

            return GrantBuildResult.Ok(grant);
        }

        public async Task<ApiResponse<PermissionGrantDto>> CreateDirectGrantAsync(
            Guid organizationId,
            Guid grantedByUserId,
            CreatePermissionGrantDto request,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(request.SubjectId, out var employeeId))
            {
                return ApiResponse<PermissionGrantDto>.FailureResponse("Employee id is not valid.", "معرّف الموظف غير صالح");
            }

            var employee = await _db.Employees
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == employeeId, cancellationToken);

            if (employee == null)
            {
                return ApiResponse<PermissionGrantDto>.FailureResponse("Employee was not found.", "الموظف غير موجود");
            }

            var actionExists = await _db.PermissionActions
                .AnyAsync(x => x.Key == request.ActionKey && x.IsEnabled, cancellationToken);

            if (!actionExists)
            {
                return ApiResponse<PermissionGrantDto>.FailureResponse("Permission action was not found.", "إجراء الصلاحية غير موجود");
            }

            var subjectId = employee.Id.ToString();

            // Guard against stacking a second overlapping grant for the same action/subject — the admin
            // must revoke the existing one first rather than creating ambiguous, hard-to-audit duplicates.
            var nowUtc = DateTimeOffset.UtcNow;
            var existingGrants = await _db.PermissionGrants
                .Where(x => x.OrganizationId == organizationId && x.SubjectId == subjectId && x.ActionKey == request.ActionKey && x.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            var hasActiveOrPending = existingGrants
                .Any(x => PermissionGrantRuntimeStatus.Compute(x.RevokedAtUtc, x.ExpiresAtUtc, x.StartsAtUtc, nowUtc) is "Active" or "Pending");

            if (hasActiveOrPending)
            {
                return ApiResponse<PermissionGrantDto>.FailureResponse(
                    "This employee already has an active or pending grant for this action. Revoke it first before granting again.",
                    "يوجد لدى هذا الموظف بالفعل منحة نشطة أو معلقة لهذا الإجراء. الرجاء إلغاؤها أولًا قبل المنح مرة أخرى");
            }

            int decisionId;
            int subjectTypeId;

            try
            {
                decisionId = await _lookupService.GetPermissionDecisionId("Allow", cancellationToken);
                subjectTypeId = await _lookupService.GetPermissionSubjectTypeId("Employee", cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return ApiResponse<PermissionGrantDto>.FailureResponse("Required reference data was not found.", "بيانات مرجعية مطلوبة غير موجودة");
            }

            var buildResult = await BuildGrantAsync(
                organizationId,
                request.ActionKey,
                decisionId,
                subjectTypeId,
                subjectId,
                targetDeviceId: null,
                request.GrantType,
                request.StartsAtUtc,
                request.ExpiresAtUtc,
                request.Reason,
                grantedByUserId,
                sourcePermissionRequestId: null,
                cancellationToken);

            if (!buildResult.Success)
            {
                return ApiResponse<PermissionGrantDto>.FailureResponse(buildResult.ErrorMessageEn!, buildResult.ErrorMessageAr!);
            }

            var grant = buildResult.Grant!;

            await _policyVersionService.BumpAsync(
                organizationId,
                grantedByUserId,
                "GrantCreatedDirect",
                "PermissionGrant",
                grant.Id,
                $"Permission '{grant.ActionKey}' granted directly to subject {grant.SubjectId}.",
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            var savedGrant = await _db.PermissionGrants
                .AsNoTracking()
                .Include(x => x.Decision)
                .Include(x => x.SubjectType)
                .Include(x => x.GrantType)
                .Include(x => x.GrantedByUser)
                .Include(x => x.RevokedByUser)
                .Include(x => x.TargetDevice)
                .FirstAsync(x => x.Id == grant.Id, cancellationToken);

            var employeeNames = new Dictionary<Guid, string> { [employee.Id] = employee.DisplayName };
            var dto = MapToDto(savedGrant, DateTimeOffset.UtcNow, employeeNames);

            return ApiResponse<PermissionGrantDto>.SuccessResponse(dto, "Permission granted successfully.", "تم منح الصلاحية بنجاح");
        }

        private async Task<bool> IsEmployeeUserTypeAsync(int userTypeId, CancellationToken cancellationToken)
        {
            var employeeUserType = await _db.UserTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Employee", cancellationToken);

            return employeeUserType != null && userTypeId == employeeUserType.Id;
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
