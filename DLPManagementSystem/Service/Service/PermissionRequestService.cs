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
            UpdatedAtUtc = x.UpdatedAtUtc
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

        public PermissionRequestService(DLPSystemContext db, IPermissionLookupService lookupService, IPolicyVersionService policyVersionService)
        {
            _db = db;
            _lookupService = lookupService;
            _policyVersionService = policyVersionService;
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

            if (await IsEmployeeUserTypeAsync(callerUserTypeId, cancellationToken))
            {
                // Employee-type callers can only ever see their own requests — force this regardless of
                // whatever requestedByEmployeeId the client passed.
                var callerEmployee = await _db.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == callerUserId, cancellationToken);

                if (callerEmployee == null)
                {
                    return ApiResponse<PagedResultDto<PermissionRequestDto>>.SuccessResponse(new PagedResultDto<PermissionRequestDto>
                    {
                        Items = new List<PermissionRequestDto>(),
                        TotalCount = 0,
                        Page = page,
                        PageSize = pageSize
                    });
                }

                effectiveRequestedByEmployeeId = callerEmployee.Id;
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

        public async Task<ApiResponse<PermissionRequestDto>> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
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

            PopulateGrantRuntimeStatus(dto, DateTimeOffset.UtcNow);

            return ApiResponse<PermissionRequestDto>.SuccessResponse(dto);
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
                SubmittedAtUtc = nowUtc,
                CreatedAtUtc = nowUtc
            };

            _db.PermissionRequests.Add(permissionRequest);
            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(organizationId, permissionRequest.Id, cancellationToken);
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

            int grantTypeId;
            string grantTypeName;

            if (!string.IsNullOrWhiteSpace(request.GrantType))
            {
                try
                {
                    grantTypeId = await _lookupService.GetPermissionGrantTypeId(request.GrantType, cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    return ApiResponse<PermissionRequestDto>.FailureResponse("Required reference data was not found.", "بيانات مرجعية مطلوبة غير موجودة");
                }

                grantTypeName = request.GrantType;
            }
            else
            {
                grantTypeId = permissionRequest.RequestedGrantTypeId;
                grantTypeName = permissionRequest.RequestedGrantType.Name;
            }

            var startsAtUtc = request.StartsAtUtc ?? permissionRequest.RequestedStartsAtUtc ?? nowUtc;
            DateTimeOffset? expiresAtUtc;

            if (string.Equals(grantTypeName, "Temporary", StringComparison.OrdinalIgnoreCase))
            {
                expiresAtUtc = request.ExpiresAtUtc ?? permissionRequest.RequestedExpiresAtUtc;

                if (expiresAtUtc == null || expiresAtUtc <= startsAtUtc)
                {
                    return ApiResponse<PermissionRequestDto>.FailureResponse(
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
                ActionKey = permissionRequest.ActionKey,
                DecisionId = permissionRequest.RequestedDecisionId,
                SubjectTypeId = permissionRequest.SubjectTypeId,
                SubjectId = permissionRequest.SubjectId,
                TargetDeviceId = permissionRequest.TargetDeviceId,
                GrantTypeId = grantTypeId,
                Priority = 500,
                StartsAtUtc = startsAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                Reason = permissionRequest.BusinessJustification,
                GrantedByUserId = reviewedByUserId,
                SourcePermissionRequestId = permissionRequest.Id,
                CreatedAtUtc = nowUtc
            };

            _db.PermissionGrants.Add(grant);

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

            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(organizationId, id, cancellationToken);
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

            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(organizationId, id, cancellationToken);
        }

        private async Task<bool> IsEmployeeUserTypeAsync(int userTypeId, CancellationToken cancellationToken)
        {
            var employeeUserType = await _db.UserTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Employee", cancellationToken);

            return employeeUserType != null && userTypeId == employeeUserType.Id;
        }
    }
}
