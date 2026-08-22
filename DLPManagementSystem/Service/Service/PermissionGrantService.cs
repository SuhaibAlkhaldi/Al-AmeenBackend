using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;
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
        private readonly IAdminAuditLogService _adminAuditLogService;

        public PermissionGrantService(
            DLPSystemContext db,
            IPolicyVersionService policyVersionService,
            IPermissionLookupService lookupService,
            IAdminAuditLogService adminAuditLogService)
        {
            _db = db;
            _policyVersionService = policyVersionService;
            _lookupService = lookupService;
            _adminAuditLogService = adminAuditLogService;
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

            var targetDisplayName = await ResolveSubjectDisplayNameAsync(organizationId, grant.SubjectId, cancellationToken);
            await _adminAuditLogService.LogAsync(
                organizationId, revokedByUserId, "GrantRevoked", "PermissionGrant", grant.Id, targetDisplayName,
                $"Action '{grant.ActionKey}'. Reason: {request.RevocationReason}", cancellationToken);

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

            var bulkTargetDisplayName = await ResolveSubjectDisplayNameAsync(organizationId, request.SubjectId, cancellationToken);
            await _adminAuditLogService.LogAsync(
                organizationId, revokedByUserId, "GrantsBulkRevoked", "PermissionGrant", null, bulkTargetDisplayName,
                $"{toRevoke.Count} grant(s) revoked: {string.Join(", ", toRevoke.Select(x => x.ActionKey))}. Reason: {request.RevocationReason}",
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
            CancellationToken cancellationToken = default,
            string? fileHash = null,
            string? classificationTier = null)
        {
            var nowUtc = DateTimeOffset.UtcNow;

            // Guard against stacking a second overlapping grant for the same action/subject/scope — lives
            // here (rather than in each caller) so every current and future path into a grant creation
            // gets this protection automatically. The DB's three filtered unique indexes
            // (UX_PermissionGrants_Active_Subject_Action[_FileHash|_Tier]) enforce the same "one
            // non-revoked row per subject+action+scope" rule at the storage layer — this in-memory check
            // must agree with them on every non-revoked row, not just Active/Pending ones. Scope equality
            // (both FileHash and ClassificationTier matching) is what makes an existing grant relevant:
            // an unscoped, a FileHash="abc", and a ClassificationTier="Secret" grant for the same
            // employee+action can all coexist, since each is backed by its own unique index.
            var existingGrants = await _db.PermissionGrants
                .Where(x => x.OrganizationId == organizationId
                    && x.SubjectId == subjectId
                    && x.ActionKey == actionKey
                    && x.RevokedAtUtc == null
                    && x.FileHash == fileHash
                    && x.ClassificationTier == classificationTier)
                .ToListAsync(cancellationToken);

            var blockingGrants = existingGrants
                .Where(x => PermissionGrantRuntimeStatus.Compute(x.RevokedAtUtc, x.ExpiresAtUtc, x.StartsAtUtc, nowUtc) is "Active" or "Pending")
                .ToList();

            if (blockingGrants.Count > 0)
            {
                return GrantBuildResult.Failure(
                    "This employee already has an active or pending grant for this action. Revoke it first before granting again.",
                    "يوجد لدى هذا الموظف بالفعل منحة نشطة أو معلقة لهذا الإجراء. الرجاء إلغاؤها أولًا قبل المنح مرة أخرى");
            }

            // Any remaining non-revoked rows here are Expired — they convey no real ongoing access, so
            // requiring a manual "revoke the thing that already ended" step before granting again would be
            // pure friction. Auto-clear them so the DB's unique index and this check stay in agreement.
            foreach (var staleExpiredGrant in existingGrants)
            {
                ApplyRevocation(staleExpiredGrant, grantedByUserId, "Superseded by new grant.", nowUtc);
            }

            int grantTypeId;

            try
            {
                grantTypeId = await _lookupService.GetPermissionGrantTypeId(grantTypeName, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return GrantBuildResult.Failure("Required reference data was not found.", "بيانات مرجعية مطلوبة غير موجودة");
            }

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
                FileHash = fileHash,
                ClassificationTier = classificationTier,
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

            if (!string.IsNullOrWhiteSpace(request.ClassificationTier))
            {
                if (!request.ActionKey.Equals(PermissionActionKeys.FileDecrypt, StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<PermissionGrantDto>.FailureResponse(
                        "A classification tier can only be set for the file.decrypt action.",
                        "لا يمكن تحديد مستوى التصنيف إلا لإجراء فك تشفير الملف (file.decrypt)");
                }

                if (!ClassificationTiers.Order.Contains(request.ClassificationTier))
                {
                    return ApiResponse<PermissionGrantDto>.FailureResponse(
                        "Classification tier is not valid.", "مستوى التصنيف غير صالح");
                }
            }

            var subjectId = employee.Id.ToString();

            // Duplicate-active-grant check now lives inside BuildGrantAsync itself, so it applies here
            // automatically without a separate check.
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
                cancellationToken,
                classificationTier: request.ClassificationTier);

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

            await _adminAuditLogService.LogAsync(
                organizationId, grantedByUserId, "GrantCreated", "PermissionGrant", grant.Id, employee.DisplayName,
                $"Action '{grant.ActionKey}', grant type '{request.GrantType}'.", cancellationToken);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueConstraintViolation(ex))
            {
                // The grant row and the policy-version row this method stages both land in the same
                // SaveChangesAsync call above, so a unique-index collision here could originate from
                // either one. PolicyVersionService.BumpAsync now draws its number from a database
                // sequence (see that file), so a collision on UQ_PolicyVersions_Organization_Version
                // shouldn't happen anymore in practice - but if it (or any other unrelated unique-index
                // collision) ever did, reporting "this employee already has an active grant" would be
                // actively misleading: that message is only true for the specific duplicate-active-grant
                // indexes below, not for a coincidental collision on something else entirely.
                if (DbExceptionHelper.IsUniqueConstraintViolationOfIndex(ex, "UQ_PolicyVersions_Organization_Version"))
                {
                    return ApiResponse<PermissionGrantDto>.FailureResponse(
                        "A temporary conflict occurred while saving. Please try again.",
                        "حدث تعارض مؤقت أثناء الحفظ. يرجى المحاولة مرة أخرى");
                }

                return ApiResponse<PermissionGrantDto>.FailureResponse(
                    "This employee already has an active or pending grant for this action. Revoke it first before granting again.",
                    "يوجد لدى هذا الموظف بالفعل منحة نشطة أو معلقة لهذا الإجراء. الرجاء إلغاؤها أولًا قبل المنح مرة أخرى");
            }

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

        private async Task<string?> ResolveSubjectDisplayNameAsync(Guid organizationId, string subjectId, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(subjectId, out var subjectGuid))
            {
                return subjectId;
            }

            var employee = await _db.Employees
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == subjectGuid)
                .Select(x => x.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);

            return employee ?? subjectId;
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
                RevokedByUserName = grant.RevokedByUser?.FullName,
                ClassificationTier = grant.ClassificationTier
            };
        }
    }
}
