using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.DemoRequests;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class DemoRequestService : IDemoRequestService
    {
        private const string NewStatusName = "New";

        private readonly DLPSystemContext _db;
        private readonly IDemoRequestEmailService _emailService;
        private readonly ILogger<DemoRequestService> _logger;

        public DemoRequestService(DLPSystemContext db, IDemoRequestEmailService emailService, ILogger<DemoRequestService> logger)
        {
            _db = db;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ApiResponse<DemoRequestListItemDto>> CreateAsync(
            CreateDemoRequestDto request, string? sourceIp, CancellationToken cancellationToken = default)
        {
            var newStatus = await _db.DemoRequestStatuses
                .FirstOrDefaultAsync(x => x.Name == NewStatusName, cancellationToken);

            if (newStatus == null)
            {
                // Seed data missing is a server-side configuration bug, not something the caller
                // (an anonymous website visitor) can act on — keep the message generic.
                _logger.LogError("DemoRequestStatus seed row {StatusName} is missing.", NewStatusName);
                return ApiResponse<DemoRequestListItemDto>.FailureResponse(
                    "Unable to submit your request right now. Please try again later.",
                    "تعذّر إرسال طلبك حاليًا، يرجى المحاولة لاحقًا.");
            }

            var demoRequest = new DemoRequest
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                CompanyEmail = request.CompanyEmail.Trim(),
                CompanyName = request.CompanyName.Trim(),
                CompanySize = request.CompanySize.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                StatusId = newStatus.Id,
                SourceIp = sourceIp,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            _db.DemoRequests.Add(demoRequest);
            await _db.SaveChangesAsync(cancellationToken);

            // Best-effort: the lead is already durably saved above, so a broken/unreachable SMTP
            // server must never turn into a failure response for the public form submission.
            try
            {
                await _emailService.SendNewDemoRequestNotificationAsync(demoRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send demo request notification email for DemoRequest {DemoRequestId}.", demoRequest.Id);
            }

            var dto = MapToListItem(demoRequest, newStatus.Name);

            return ApiResponse<DemoRequestListItemDto>.SuccessResponse(
                dto,
                "Your request was received. Our team will contact you soon.",
                "تم استلام طلبك، سيتواصل فريقنا معك قريبًا.");
        }

        public async Task<ApiResponse<PagedResultDto<DemoRequestListItemDto>>> GetListAsync(
            int? statusId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _db.DemoRequests.AsNoTracking().AsQueryable();

            if (statusId.HasValue)
            {
                query = query.Where(x => x.StatusId == statusId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new DemoRequestListItemDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    CompanyEmail = x.CompanyEmail,
                    CompanyName = x.CompanyName,
                    CompanySize = x.CompanySize,
                    Phone = x.Phone,
                    StatusId = x.StatusId,
                    StatusName = x.Status.Name,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<DemoRequestListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<DemoRequestListItemDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<DemoRequestListItemDto>> UpdateStatusAsync(
            Guid id, UpdateDemoRequestStatusDto request, CancellationToken cancellationToken = default)
        {
            var demoRequest = await _db.DemoRequests
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (demoRequest == null)
            {
                return ApiResponse<DemoRequestListItemDto>.FailureResponse(
                    "Demo request was not found.", "الطلب غير موجود");
            }

            var newStatus = await _db.DemoRequestStatuses
                .FirstOrDefaultAsync(x => x.Id == request.StatusId, cancellationToken);

            if (newStatus == null)
            {
                return ApiResponse<DemoRequestListItemDto>.FailureResponse(
                    "Status was not found.", "الحالة غير موجودة");
            }

            demoRequest.StatusId = newStatus.Id;
            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<DemoRequestListItemDto>.SuccessResponse(MapToListItem(demoRequest, newStatus.Name));
        }

        private static DemoRequestListItemDto MapToListItem(DemoRequest demoRequest, string statusName)
        {
            return new DemoRequestListItemDto
            {
                Id = demoRequest.Id,
                FullName = demoRequest.FullName,
                CompanyEmail = demoRequest.CompanyEmail,
                CompanyName = demoRequest.CompanyName,
                CompanySize = demoRequest.CompanySize,
                Phone = demoRequest.Phone,
                StatusId = demoRequest.StatusId,
                StatusName = statusName,
                CreatedAtUtc = demoRequest.CreatedAtUtc
            };
        }
    }
}
