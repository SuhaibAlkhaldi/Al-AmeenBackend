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
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        };

        private readonly DLPSystemContext _db;
        private readonly IPermissionLookupService _lookupService;

        public PermissionRequestService(DLPSystemContext db, IPermissionLookupService lookupService)
        {
            _db = db;
            _lookupService = lookupService;
        }

        public async Task<ApiResponse<PagedResultDto<PermissionRequestDto>>> GetRequestsAsync(
            Guid organizationId,
            int? statusId,
            Guid? requestedByEmployeeId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.PermissionRequests
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (statusId.HasValue)
            {
                query = query.Where(x => x.StatusId == statusId.Value);
            }

            if (requestedByEmployeeId.HasValue)
            {
                query = query.Where(x => x.RequestedByEmployeeId == requestedByEmployeeId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToDto)
                .ToListAsync(cancellationToken);

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

            return ApiResponse<PermissionRequestDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<PermissionRequestDto>> CreateAsync(
            Guid organizationId,
            Guid requestedByUserId,
            CreatePermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
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
            catch (InvalidOperationException ex)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(ex.Message, "بيانات مرجعية مطلوبة غير موجودة");
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
            ReviewPermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var permissionRequest = await _db.PermissionRequests
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
            catch (InvalidOperationException ex)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(ex.Message, "بيانات مرجعية مطلوبة غير موجودة");
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
                GrantTypeId = permissionRequest.RequestedGrantTypeId,
                Priority = 500,
                StartsAtUtc = permissionRequest.RequestedStartsAtUtc ?? nowUtc,
                ExpiresAtUtc = permissionRequest.RequestedExpiresAtUtc,
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

            await _db.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(organizationId, id, cancellationToken);
        }

        public async Task<ApiResponse<PermissionRequestDto>> RejectAsync(
            Guid organizationId,
            Guid id,
            Guid reviewedByUserId,
            ReviewPermissionRequestDto request,
            CancellationToken cancellationToken = default)
        {
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
            catch (InvalidOperationException ex)
            {
                return ApiResponse<PermissionRequestDto>.FailureResponse(ex.Message, "بيانات مرجعية مطلوبة غير موجودة");
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

    }
}
