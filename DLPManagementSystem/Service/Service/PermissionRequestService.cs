using System.Linq.Expressions;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class PermissionRequestService : IPermissionRequestService
    {
        private static readonly Expression<Func<PermissionRequest, PermissionRequestDto>> ToDto = x => new PermissionRequestDto
        {
            Id = x.Id,
            OrganizationId = x.OrganizationId,
            RequestedByUserId = x.RequestedByUserId,
            RequestedByEmployeeId = x.RequestedByEmployeeId,
            RequestedByEmployeeName = x.RequestedByEmployee.DisplayName,
            ActionKey = x.ActionKey,
            RequestedDecision = x.RequestedDecision.Name,
            RequestedGrantType = x.RequestedGrantType.Name,
            SubjectType = x.SubjectType.Name,
            SubjectId = x.SubjectId,
            TargetDeviceId = x.TargetDeviceId,
            TargetDeviceName = x.TargetDevice != null ? x.TargetDevice.MachineName : null,
            RequestedStartsAtUtc = x.RequestedStartsAtUtc,
            RequestedExpiresAtUtc = x.RequestedExpiresAtUtc,
            RequestedDurationMinutes = x.RequestedDurationMinutes,
            BusinessJustification = x.BusinessJustification,
            Status = x.Status.Name,
            SubmittedAtUtc = x.SubmittedAtUtc,
            ReviewedByUserId = x.ReviewedByUserId,
            ReviewedByUserName = x.ReviewedByUser != null ? x.ReviewedByUser.FullName : null,
            ReviewedAtUtc = x.ReviewedAtUtc,
            ReviewDecision = x.ReviewDecision != null ? x.ReviewDecision.Name : null,
            ReviewNotes = x.ReviewNotes,
            ResultPermissionGrantId = x.ResultPermissionGrantId,
            GrantedGrantType = x.ResultPermissionGrant != null ? x.ResultPermissionGrant.GrantType.Name : null,
            GrantedStartsAtUtc = x.ResultPermissionGrant != null ? x.ResultPermissionGrant.StartsAtUtc : (DateTimeOffset?)null,
            GrantedExpiresAtUtc = x.ResultPermissionGrant != null ? x.ResultPermissionGrant.ExpiresAtUtc : null,
            GrantRevokedAtUtc = x.ResultPermissionGrant != null ? x.ResultPermissionGrant.RevokedAtUtc : null,
            GrantRevocationReason = x.ResultPermissionGrant != null ? x.ResultPermissionGrant.RevocationReason : null,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
            ClassificationTier = x.ClassificationTier,
            FileName = x.PermissionRequestAttachments.Select(a => a.FileName).FirstOrDefault(),
            FileSizeBytes = x.PermissionRequestAttachments.Select(a => (long?)a.SizeInBytes).FirstOrDefault(),
            FileSha256 = x.PermissionRequestAttachments.Select(a => a.Sha256Hash).FirstOrDefault(),
            FileClassification = x.PermissionRequestAttachments.Select(a => a.Classification).FirstOrDefault(),
            FileClassificationReasonCode = x.PermissionRequestAttachments.Select(a => a.ClassificationReasonCode).FirstOrDefault()
        };

        private static void PopulateGrantRuntimeStatus(PermissionRequestDto dto, DateTimeOffset nowUtc)
        {
            if (dto.GrantedStartsAtUtc.HasValue)
            {
                dto.GrantRuntimeStatus = PermissionGrantRuntimeStatus.Compute(dto.GrantRevokedAtUtc, dto.GrantedExpiresAtUtc, dto.GrantedStartsAtUtc.Value, nowUtc);
            }
        }

        private readonly DLPSystemContext _db;
        private readonly IPermissionLookupService _lookupService;
        private readonly IPolicyVersionService _policyVersionService;
        private readonly IPermissionGrantService _permissionGrantService;
        private readonly IAdminAuditLogService _adminAuditLogService;

        public PermissionRequestService(
            DLPSystemContext db,
            IPermissionLookupService lookupService,
            IPolicyVersionService policyVersionService,
            IPermissionGrantService permissionGrantService,
            IAdminAuditLogService adminAuditLogService)
        {
            _db = db;
            _lookupService = lookupService;
            _policyVersionService = policyVersionService;
            _permissionGrantService = permissionGrantService;
            _adminAuditLogService = adminAuditLogService;
        }

        public async Task<ApiResponse<PagedResultDto<PermissionRequestDto>>> GetRequestsAsync(
            Guid organizationId,
            Guid callerUserId,
            int callerUserTypeId,
            int? statusId,
            Guid? requestedByEmployeeId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var effectiveRequestedByEmployeeId = requestedByEmployeeId;
            var (isEmployee, callerEmployeeId) = await ResolveEmployeeScopeAsync(organizationId, callerUserId, callerUserTypeId, cancellationToken);

            if (isEmployee)
            {
                // Employee-type callers can only ever see their own requests — force this regardless of
                // whatever requestedByEmployeeId the client passed.
                if (callerEmployeeId == null)
                {
                    return ApiResponse<PagedResultDto<PermissionRequestDto>>.SuccessResponse(new PagedResultDto<PermissionRequestDto>
                    {
                        Items = new List<PermissionRequestDto>(),
                        TotalCount = 0,
                        Page = page,
                        PageSize = pageSize
                    });
                }

                effectiveRequestedByEmployeeId = callerEmployeeId;
            }

            var query = _db.PermissionRequests
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (statusId.HasValue)
            {
                query = query.Where(x => x.StatusId == statusId.Value);
            }

            if (effectiveRequestedByEmployeeId.HasValue)
            {
                query = query.Where(x => x.RequestedByEmployeeId == effectiveRequestedByEmployeeId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToDto)
                .ToListAsync(cancellationToken);

            var nowUtc = DateTimeOffset.UtcNow;
            foreach (var item in items)
            {
                PopulateGrantRuntimeStatus(item, nowUtc);
            }

            var result = new PagedResultDto<PermissionRequestDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<PermissionRequestDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<int>> GetPendingCountAsync(
            Guid organizationId,
            Guid callerUserId,
            int callerUserTypeId,
            CancellationToken cancellationToken = default)
        {
            var (isEmployee, callerEmployeeId) = await ResolveEmployeeScopeAsync(organizationId, callerUserId, callerUserTypeId, cancellationToken);

            if (isEmployee && callerEmployeeId == null)
            {
                return ApiResponse<int>.SuccessResponse(0);
            }

            var pendingStatusIds = await _db.PermissionRequestStatuses
                .AsNoTracking()
                .Where(x => x.Name == "Submitted" || x.Name == "UnderReview")
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var query = _db.PermissionRequests
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && pendingStatusIds.Contains(x.StatusId));

            if (isEmployee)
            {
                query = query.Where(x => x.RequestedByEmployeeId == callerEmployeeId!.Value);
            }

            var count = await query.CountAsync(cancellationToken);

            return ApiResponse<int>.SuccessResponse(count);
        }

        public async Task<ApiResponse<PermissionRequestDto>> GetByIdAsync(
            Guid organizationId,
            Guid id,
            Guid callerUserId,
            int callerUserTypeId,
            CancellationToken cancellationToken = default)
        {
            var dto = await _db.PermissionRequests
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == id)
                .Select(ToDto)
                .FirstOrDefaultAsync(cancellationToken);

            if (dto == null)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse("Permission request was not found.", "طلب الصلاحية غير موجود");
            }

            if (await IsEmployeeUserTypeAsync(callerUserTypeId, cancellationToken))
            {
                // Employee-type callers may only view their own request — same restriction GetRequestsAsync
                // already applies to the list endpoint, mirrored here so a guessed/known GUID doesn't bypass it.
                var callerEmployee = await _db.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == callerUserId, cancellationToken);

                if (callerEmployee == null || dto.RequestedByEmployeeId != callerEmployee.Id)
                {
                    return ApiResponse<PermissionRequestDto>.FailureResponse("Permission request was not found.", "طلب الصلاحية غير موجود");
                }
            }

            PopulateGrantRuntimeStatus(dto, DateTimeOffset.UtcNow);

            return ApiResponse<PermissionRequestDto>.SuccessResponse(dto);
        }

        // The action keys the agent's file classification cache/scanner covers - the exact-file
        // request path only ever makes sense for these, so the prefill lookup below is scoped to
        // just them rather than any action key. file.print added alongside the browser actions: a
        // blocked print attempt reports the printed file's hash/classification on its AuditEvent
        // the same way a blocked upload does (see CompanyDlp.Service's PrintProtectionMonitor).
        private static readonly string[] FileScopedActionKeys = ["browser.upload", "browser.drag-drop", "file.print"];

        public async Task<ApiResponse<SourceEventDetailsDto>> GetSourceEventDetailsAsync(
            Guid organizationId,
            Guid callerUserId,
            int callerUserTypeId,
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            if (!await IsEmployeeUserTypeAsync(callerUserTypeId, cancellationToken))
            {
                return ApiResponse<SourceEventDetailsDto>.FailureResponse(
                    "Only Employee accounts are permitted to view blocked-attempt details.",
                    "فقط حسابات الموظفين مخوّلة بعرض تفاصيل محاولات الحظر");
            }

            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == callerUserId, cancellationToken);

            if (employee == null)
            {
                return ApiResponse<SourceEventDetailsDto>.FailureResponse(
                    "The current user does not have a linked employee profile.",
                    "المستخدم الحالي لا يملك ملف موظف مرتبط");
            }

            var auditEvent = await _db.AuditEvents
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId
                    && x.CorrelationId == correlationId
                    && x.EmployeeId == employee.Id
                    && FileScopedActionKeys.Contains(x.ActionKey))
                .OrderByDescending(x => x.OccurredAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (auditEvent == null)
            {
                return ApiResponse<SourceEventDetailsDto>.FailureResponse(
                    "The referenced blocked attempt was not found.",
                    "محاولة الحظر المشار إليها غير موجودة");
            }

            var fileDetails = ParseResourceFromMetadata(auditEvent.MetadataJson);
            if (fileDetails == null)
            {
                return ApiResponse<SourceEventDetailsDto>.FailureResponse(
                    "The referenced blocked attempt has no recorded file details.",
                    "محاولة الحظر المشار إليها لا تحتوي على تفاصيل ملف مسجّلة");
            }

            return ApiResponse<SourceEventDetailsDto>.SuccessResponse(new SourceEventDetailsDto
            {
                CorrelationId = correlationId,
                ActionKey = auditEvent.ActionKey,
                FileName = fileDetails.FileName,
                SizeBytes = fileDetails.SizeBytes,
                Sha256 = fileDetails.Sha256,
                Classification = fileDetails.Classification,
                ClassificationReasonCode = fileDetails.ClassificationReasonCode,
                OccurredAtUtc = auditEvent.OccurredAtUtc
            });
        }

        public async Task<ApiResponse<PermissionRequestDto>> CreateAsync(
            Guid organizationId,
            Guid requestedByUserId,
            int callerUserTypeId,
            CreatePermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (!await IsEmployeeUserTypeAsync(callerUserTypeId, cancellationToken))
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(
                    "Only Employee accounts are permitted to submit permission requests.",
                    "فقط حسابات الموظفين مخوّلة بتقديم طلبات الصلاحيات");
            }

            var employee = await _db.Employees
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == requestedByUserId, cancellationToken);

            if (employee == null)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(
                    "The current user does not have a linked employee profile.",
                    "المستخدم الحالي لا يملك ملف موظف مرتبط");
            }

            var actionExists = await _db.PermissionActions
                .AnyAsync(x => x.Key == request.ActionKey && x.IsEnabled, cancellationToken);

            if (!actionExists)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse("Permission action was not found.", "إجراء الصلاحية غير موجود");
            }

            if (request.SourceCorrelationId.HasValue && !string.IsNullOrWhiteSpace(request.ClassificationTier))
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(
                    "A request cannot target a specific file and a classification tier at the same time.",
                    "لا يمكن أن يستهدف الطلب ملفًا محددًا وتصنيفًا عامًا في نفس الوقت");
            }

            string? classificationTier = null;
            SourceEventFileDetails? fileDetails = null;

            if (!string.IsNullOrWhiteSpace(request.ClassificationTier))
            {
                if (!DTO.AgentFiles.ClassificationTiers.IsValidRequestableTier(request.ClassificationTier))
                {
                    return ApiResponse<PermissionRequestDto>.FailureResponse(
                        "Classification tier must be Internal, Secret, or Very_Secret.",
                        "يجب أن يكون تصنيف الملف مقيد أو سري أو سري للغاية");
                }

                classificationTier = request.ClassificationTier;
            }
            else if (request.SourceCorrelationId.HasValue)
            {
                var sourceEventResult = await ResolveSourceEventFileDetailsAsync(
                    organizationId, employee.Id, request.ActionKey, request.SourceCorrelationId.Value, cancellationToken);

                if (sourceEventResult == null)
                {
                    return ApiResponse<PermissionRequestDto>.FailureResponse(
                        "The referenced blocked attempt was not found.",
                        "محاولة الحظر المشار إليها غير موجودة");
                }

                fileDetails = sourceEventResult;
            }

            int requestedGrantTypeId;
            int requestedDecisionId;
            int subjectTypeId;
            int statusId;

            try
            {
                requestedGrantTypeId = await _lookupService.GetPermissionGrantTypeId(request.GrantType, cancellationToken);
                requestedDecisionId = await _lookupService.GetPermissionDecisionId("Allow", cancellationToken);
                subjectTypeId = await _lookupService.GetPermissionSubjectTypeId("Employee", cancellationToken);
                statusId = await _lookupService.GetPermissionRequestStatusId("Submitted", cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse("Required reference data was not found.", "بيانات مرجعية مطلوبة غير موجودة");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var permissionRequest = new PermissionRequest
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RequestedByUserId = requestedByUserId,
                RequestedByEmployeeId = employee.Id,
                ActionKey = request.ActionKey,
                RequestedDecisionId = requestedDecisionId,
                RequestedGrantTypeId = requestedGrantTypeId,
                SubjectTypeId = subjectTypeId,
                SubjectId = employee.Id.ToString(),
                TargetDeviceId = null,
                RequestedStartsAtUtc = request.RequestedStartsAtUtc,
                RequestedExpiresAtUtc = request.RequestedExpiresAtUtc,
                BusinessJustification = request.BusinessJustification,
                StatusId = statusId,
                ClassificationTier = classificationTier,
                SubmittedAtUtc = nowUtc,
                CreatedAtUtc = nowUtc
            };

            _db.PermissionRequests.Add(permissionRequest);

            if (fileDetails != null)
            {
                _db.PermissionRequestAttachments.Add(new PermissionRequestAttachment
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    PermissionRequestId = permissionRequest.Id,
                    UploadedByUserId = requestedByUserId,
                    FileName = fileDetails.FileName,
                    OriginalFileName = fileDetails.FileName,
                    ContentType = null,
                    SizeInBytes = fileDetails.SizeBytes,
                    // No file content is ever transferred as part of submitting a request — this row
                    // describes a file the agent already observed and classified, not an uploaded one,
                    // so there is no real file on disk for this path to point to.
                    StoragePath = $"agent-observed:{request.SourceCorrelationId}",
                    Sha256Hash = fileDetails.Sha256,
                    Classification = fileDetails.Classification,
                    ClassificationReasonCode = fileDetails.ClassificationReasonCode,
                    CreatedAtUtc = nowUtc
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(organizationId, permissionRequest.Id, requestedByUserId, callerUserTypeId, cancellationToken);
        }

        private sealed record SourceEventFileDetails(
            string FileName,
            long SizeBytes,
            string Sha256,
            string? Classification,
            string? ClassificationReasonCode);

        private sealed class AuditMetadataEnvelope
        {
            public AuditMetadataResource? Resource { get; set; }
        }

        private sealed class AuditMetadataResource
        {
            public string? Name { get; set; }
            public string? Extension { get; set; }
            public long? SizeBytes { get; set; }
            public string? Sha256 { get; set; }
            public string? Classification { get; set; }
            public string? ClassificationReasonCode { get; set; }
        }

        // Never trusts client-supplied file details — looks the blocked attempt up server-side by
        // CorrelationId and verifies it actually belongs to the requesting employee, so a guessed or
        // reused correlation id from a different employee's blocked attempt can't be used to spoof a
        // request. Returns null for "not found or not owned by this employee" (both collapse to the
        // same caller-visible failure, same pattern as GetByIdAsync's employee-scoping check).
        private async Task<SourceEventFileDetails?> ResolveSourceEventFileDetailsAsync(
            Guid organizationId,
            Guid employeeId,
            string actionKey,
            Guid correlationId,
            CancellationToken cancellationToken)
        {
            var auditEvent = await _db.AuditEvents
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId
                    && x.CorrelationId == correlationId
                    && x.ActionKey == actionKey
                    && x.EmployeeId == employeeId)
                .OrderByDescending(x => x.OccurredAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            return auditEvent == null ? null : ParseResourceFromMetadata(auditEvent.MetadataJson);
        }

        private static SourceEventFileDetails? ParseResourceFromMetadata(string? metadataJson)
        {
            if (string.IsNullOrWhiteSpace(metadataJson))
            {
                return null;
            }

            AuditMetadataEnvelope? metadata;
            try
            {
                metadata = System.Text.Json.JsonSerializer.Deserialize<AuditMetadataEnvelope>(
                    metadataJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }

            var resource = metadata?.Resource;
            if (resource == null || string.IsNullOrWhiteSpace(resource.Sha256))
            {
                return null;
            }

            return new SourceEventFileDetails(
                string.IsNullOrWhiteSpace(resource.Name) ? "unknown" : resource.Name,
                resource.SizeBytes ?? 0,
                resource.Sha256,
                string.IsNullOrWhiteSpace(resource.Classification) ? null : resource.Classification,
                string.IsNullOrWhiteSpace(resource.ClassificationReasonCode) ? null : resource.ClassificationReasonCode);
        }

        public async Task<ApiResponse<PermissionRequestDto>> ApproveAsync(
            Guid organizationId,
            Guid id,
            Guid reviewedByUserId,
            int callerUserTypeId,
            ReviewPermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (await IsEmployeeUserTypeAsync(callerUserTypeId, cancellationToken))
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(
                    "Employee accounts are not permitted to approve permission requests.",
                    "حسابات الموظفين غير مخوّلة للموافقة على طلبات الصلاحيات");
            }

            var permissionRequest = await _db.PermissionRequests
                .Include(x => x.RequestedGrantType)
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (permissionRequest == null)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse("Permission request was not found.", "طلب الصلاحية غير موجود");
            }

            // Exact-file path: the request's linked attachment (created in CreateAsync from the source
            // AuditEvent) carries the file's hash, which scopes the resulting grant to that one file.
            // Tier path: permissionRequest.ClassificationTier scopes it instead. The two are mutually
            // exclusive by construction (CreateAsync never sets both), so at most one is non-null here.
            var fileHash = await _db.PermissionRequestAttachments
                .AsNoTracking()
                .Where(x => x.PermissionRequestId == permissionRequest.Id)
                .Select(x => x.Sha256Hash)
                .FirstOrDefaultAsync(cancellationToken);

            var nowUtc = DateTimeOffset.UtcNow;

            int approvedStatusId;
            int approvedReviewDecisionId;

            try
            {
                approvedStatusId = await _lookupService.GetPermissionRequestStatusId("Approved", cancellationToken);
                approvedReviewDecisionId = await _lookupService.GetPermissionRequestReviewDecisionId("Approved", cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse("Required reference data was not found.", "بيانات مرجعية مطلوبة غير موجودة");
            }

            var grantTypeName = !string.IsNullOrWhiteSpace(request.GrantType)
                ? request.GrantType
                : permissionRequest.RequestedGrantType.Name;

            // Shared with the admin direct-grant path (PermissionGrantService.BuildGrantAsync) so both ways of
            // creating a PermissionGrant enforce identical validation and can never drift apart.
            var buildResult = await _permissionGrantService.BuildGrantAsync(
                organizationId,
                permissionRequest.ActionKey,
                permissionRequest.RequestedDecisionId,
                permissionRequest.SubjectTypeId,
                permissionRequest.SubjectId,
                permissionRequest.TargetDeviceId,
                grantTypeName,
                request.StartsAtUtc ?? permissionRequest.RequestedStartsAtUtc,
                request.ExpiresAtUtc ?? permissionRequest.RequestedExpiresAtUtc,
                permissionRequest.BusinessJustification,
                reviewedByUserId,
                permissionRequest.Id,
                cancellationToken,
                fileHash: fileHash,
                classificationTier: permissionRequest.ClassificationTier);

            if (!buildResult.Success)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(buildResult.ErrorMessageEn!, buildResult.ErrorMessageAr!);
            }

            var grant = buildResult.Grant!;

            permissionRequest.StatusId = approvedStatusId;
            permissionRequest.ReviewDecisionId = approvedReviewDecisionId;
            permissionRequest.ReviewedByUserId = reviewedByUserId;
            permissionRequest.ReviewedAtUtc = nowUtc;
            permissionRequest.ReviewNotes = request.ReviewNotes;
            permissionRequest.ResultPermissionGrantId = grant.Id;
            permissionRequest.UpdatedAtUtc = nowUtc;

            await _policyVersionService.BumpAsync(
                organizationId,
                reviewedByUserId,
                "GrantApproved",
                "PermissionGrant",
                grant.Id,
                $"Permission '{grant.ActionKey}' approved for subject {grant.SubjectId}.",
                cancellationToken);

            var approvedTargetDisplayName = await ResolveEmployeeDisplayNameAsync(organizationId, permissionRequest.RequestedByEmployeeId, cancellationToken);
            await _adminAuditLogService.LogAsync(
                organizationId, reviewedByUserId, "RequestApproved", "PermissionRequest", permissionRequest.Id, approvedTargetDisplayName,
                $"Action '{permissionRequest.ActionKey}'. Notes: {request.ReviewNotes}", cancellationToken);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueConstraintViolation(ex))
            {
                // Race-condition safety net: the pre-insert check in BuildGrantAsync can't fully rule out
                // two concurrent approvals for the same employee+action both passing before either commits —
                // the DB's unique index is the real guard, this just turns its violation into a clean
                // bilingual failure instead of a raw 500.
                return ApiResponse<PermissionRequestDto>.FailureResponse(
                    "This employee already has an active or pending grant for this action. Revoke it first before granting again.",
                    "يوجد لدى هذا الموظف بالفعل منحة نشطة أو معلقة لهذا الإجراء. الرجاء إلغاؤها أولًا قبل المنح مرة أخرى");
            }

            return await GetByIdAsync(organizationId, id, reviewedByUserId, callerUserTypeId, cancellationToken);
        }

        public async Task<ApiResponse<PermissionRequestDto>> RejectAsync(
            Guid organizationId,
            Guid id,
            Guid reviewedByUserId,
            int callerUserTypeId,
            ReviewPermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (await IsEmployeeUserTypeAsync(callerUserTypeId, cancellationToken))
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(
                    "Employee accounts are not permitted to reject permission requests.",
                    "حسابات الموظفين غير مخوّلة لرفض طلبات الصلاحيات");
            }

            var permissionRequest = await _db.PermissionRequests
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (permissionRequest == null)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse("Permission request was not found.", "طلب الصلاحية غير موجود");
            }

            int rejectedStatusId;
            int rejectedReviewDecisionId;

            try
            {
                rejectedStatusId = await _lookupService.GetPermissionRequestStatusId("Rejected", cancellationToken);
                rejectedReviewDecisionId = await _lookupService.GetPermissionRequestReviewDecisionId("Rejected", cancellationToken);
            }
            catch (InvalidOperationException)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse("Required reference data was not found.", "بيانات مرجعية مطلوبة غير موجودة");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            permissionRequest.StatusId = rejectedStatusId;
            permissionRequest.ReviewDecisionId = rejectedReviewDecisionId;
            permissionRequest.ReviewedByUserId = reviewedByUserId;
            permissionRequest.ReviewedAtUtc = nowUtc;
            permissionRequest.ReviewNotes = request.ReviewNotes;
            permissionRequest.UpdatedAtUtc = nowUtc;

            var rejectedTargetDisplayName = await ResolveEmployeeDisplayNameAsync(organizationId, permissionRequest.RequestedByEmployeeId, cancellationToken);
            await _adminAuditLogService.LogAsync(
                organizationId, reviewedByUserId, "RequestRejected", "PermissionRequest", permissionRequest.Id, rejectedTargetDisplayName,
                $"Action '{permissionRequest.ActionKey}'. Notes: {request.ReviewNotes}", cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(organizationId, id, reviewedByUserId, callerUserTypeId, cancellationToken);
        }

        private async Task<string?> ResolveEmployeeDisplayNameAsync(Guid organizationId, Guid employeeId, CancellationToken cancellationToken)
        {
            return await _db.Employees
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == employeeId)
                .Select(x => x.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<bool> IsEmployeeUserTypeAsync(int userTypeId, CancellationToken cancellationToken)
        {
            var employeeUserType = await _db.UserTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Employee", cancellationToken);

            return employeeUserType != null && userTypeId == employeeUserType.Id;
        }

        // Shared by GetRequestsAsync and GetPendingCountAsync so the "Employee callers only ever see
        // their own" scoping rule lives in exactly one place. IsEmployee=true + EmployeeId=null means
        // the caller is an Employee-type user with no linked Employee record — callers should treat
        // that as "nothing to show" rather than falling through to an unscoped (org-wide) query.
        private async Task<(bool IsEmployee, Guid? EmployeeId)> ResolveEmployeeScopeAsync(
            Guid organizationId,
            Guid callerUserId,
            int callerUserTypeId,
            CancellationToken cancellationToken)
        {
            if (!await IsEmployeeUserTypeAsync(callerUserTypeId, cancellationToken))
            {
                return (false, null);
            }

            var callerEmployee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == callerUserId, cancellationToken);

            return (true, callerEmployee?.Id);
        }
    }
}
