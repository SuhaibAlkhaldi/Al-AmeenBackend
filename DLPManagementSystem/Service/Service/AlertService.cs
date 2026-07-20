using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Alerts;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class AlertService : IAlertService
    {
        private readonly DLPSystemContext _db;

        public AlertService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<PagedResultDto<AlertListItemDto>>> GetAlertsAsync(
            Guid organizationId,
            int? statusId,
            int? levelId,
            Guid? assignedToUserId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Alerts
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (statusId.HasValue)
            {
                query = query.Where(x => x.AlertStatusId == statusId.Value);
            }

            if (levelId.HasValue)
            {
                query = query.Where(x => x.AlertLevelId == levelId.Value);
            }

            if (assignedToUserId.HasValue)
            {
                query = query.Where(x => x.AssignedToUserId == assignedToUserId.Value);
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc <= toUtc.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AlertListItemDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    AlertLevelId = x.AlertLevelId,
                    AlertLevelName = x.AlertLevel.Name,
                    AlertStatusId = x.AlertStatusId,
                    AlertStatusName = x.AlertStatus.Name,
                    AssignedToUserId = x.AssignedToUserId,
                    AssignedToUserName = x.AssignedToUser != null ? x.AssignedToUser.FullName : null,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ClosedAtUtc = x.ClosedAtUtc,
                    IsFalsePositive = x.IsFalsePositive
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<AlertListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<AlertListItemDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<AlertDetailDto>> GetAlertByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerts
                .AsNoTracking()
                .Include(x => x.AlertLevel)
                .Include(x => x.AlertStatus)
                .Include(x => x.AssignedToUser)
                .Include(x => x.AuditEvent).ThenInclude(x => x.Device)
                .Include(x => x.AuditEvent).ThenInclude(x => x.Employee)
                .Include(x => x.AuditEvent).ThenInclude(x => x.AiAnalysisResults).ThenInclude(x => x.Decision)
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (alert == null)
            {
                return ApiResponse<AlertDetailDto>.FailureResponse("Alert was not found.", "التنبيه غير موجود");
            }

            var latestAi = alert.AuditEvent.AiAnalysisResults
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefault();

            var dto = new AlertDetailDto
            {
                Id = alert.Id,
                Title = alert.Title,
                AlertLevelId = alert.AlertLevelId,
                AlertLevelName = alert.AlertLevel.Name,
                AlertStatusId = alert.AlertStatusId,
                AlertStatusName = alert.AlertStatus.Name,
                AssignedToUserId = alert.AssignedToUserId,
                AssignedToUserName = alert.AssignedToUser?.FullName,
                CreatedAtUtc = alert.CreatedAtUtc,
                ClosedAtUtc = alert.ClosedAtUtc,
                IsFalsePositive = alert.IsFalsePositive,
                Description = alert.Description,
                InvestigationNotes = alert.InvestigationNotes,
                AuditEvent = new AlertAuditEventSummaryDto
                {
                    AuditEventId = alert.AuditEvent.Id,
                    DeviceName = alert.AuditEvent.Device.MachineName,
                    EmployeeName = alert.AuditEvent.Employee?.DisplayName,
                    ActionKey = alert.AuditEvent.ActionKey,
                    OccurredAtUtc = alert.AuditEvent.OccurredAtUtc,
                    AiDecision = latestAi?.Decision.Name,
                    AiRiskScore = latestAi?.RiskScore
                }
            };

            return ApiResponse<AlertDetailDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<AlertListItemDto>> AssignAlertAsync(Guid organizationId, Guid id, AssignAlertDto request, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerts
                .Include(x => x.AlertLevel)
                .Include(x => x.AlertStatus)
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (alert == null)
            {
                return ApiResponse<AlertListItemDto>.FailureResponse("Alert was not found.", "التنبيه غير موجود");
            }

            var assignee = await _db.Users
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == request.AssignedToUserId, cancellationToken);

            if (assignee == null)
            {
                return ApiResponse<AlertListItemDto>.FailureResponse("Assigned user was not found.", "المستخدم المعين غير موجود");
            }

            alert.AssignedToUserId = assignee.Id;
            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<AlertListItemDto>.SuccessResponse(MapToListItem(alert, assignee.FullName));
        }

        public async Task<ApiResponse<AlertListItemDto>> UpdateAlertStatusAsync(Guid organizationId, Guid id, UpdateAlertStatusDto request, CancellationToken cancellationToken = default)
        {
            var alert = await _db.Alerts
                .Include(x => x.AlertLevel)
                .Include(x => x.AssignedToUser)
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (alert == null)
            {
                return ApiResponse<AlertListItemDto>.FailureResponse("Alert was not found.", "التنبيه غير موجود");
            }

            var newStatus = await _db.AlertStatuses
                .FirstOrDefaultAsync(x => x.Id == request.AlertStatusId, cancellationToken);

            if (newStatus == null)
            {
                return ApiResponse<AlertListItemDto>.FailureResponse("Alert status was not found.", "حالة التنبيه غير موجودة");
            }

            alert.AlertStatusId = newStatus.Id;

            if (request.InvestigationNotes != null)
            {
                alert.InvestigationNotes = request.InvestigationNotes;
            }

            if (request.IsFalsePositive.HasValue)
            {
                alert.IsFalsePositive = request.IsFalsePositive.Value;
            }

            if (string.Equals(newStatus.Name, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                alert.ClosedAtUtc = DateTimeOffset.UtcNow;
            }
            else
            {
                alert.ClosedAtUtc = null;
            }

            await _db.SaveChangesAsync(cancellationToken);

            var dto = MapToListItem(alert, alert.AssignedToUser?.FullName);
            dto.AlertStatusName = newStatus.Name;

            return ApiResponse<AlertListItemDto>.SuccessResponse(dto);
        }

        private static AlertListItemDto MapToListItem(Alert alert, string? assignedToUserName)
        {
            return new AlertListItemDto
            {
                Id = alert.Id,
                Title = alert.Title,
                AlertLevelId = alert.AlertLevelId,
                AlertLevelName = alert.AlertLevel.Name,
                AlertStatusId = alert.AlertStatusId,
                AlertStatusName = alert.AlertStatus?.Name ?? string.Empty,
                AssignedToUserId = alert.AssignedToUserId,
                AssignedToUserName = assignedToUserName,
                CreatedAtUtc = alert.CreatedAtUtc,
                ClosedAtUtc = alert.ClosedAtUtc,
                IsFalsePositive = alert.IsFalsePositive
            };
        }
    }
}
