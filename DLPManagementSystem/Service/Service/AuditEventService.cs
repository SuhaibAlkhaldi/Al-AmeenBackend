using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AuditEvents;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class AuditEventService : IAuditEventService
    {
        private readonly DLPSystemContext _db;

        public AuditEventService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<PagedResultDto<AuditEventListItemDto>>> GetAuditEventsAsync(
            Guid organizationId,
            Guid? deviceId,
            Guid? employeeId,
            string? actionKey,
            int? decisionId,
            int? reasonCodeId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.AuditEvents
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (deviceId.HasValue)
            {
                query = query.Where(x => x.DeviceId == deviceId.Value);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(x => x.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(actionKey))
            {
                query = query.Where(x => x.ActionKey == actionKey);
            }

            if (decisionId.HasValue)
            {
                query = query.Where(x => x.DecisionId == decisionId.Value);
            }

            if (reasonCodeId.HasValue)
            {
                query = query.Where(x => x.ReasonCodeId == reasonCodeId.Value);
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAtUtc <= toUtc.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.OccurredAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditEventListItemDto
                {
                    Id = x.Id,
                    OccurredAtUtc = x.OccurredAtUtc,
                    ReceivedAtUtc = x.ReceivedAtUtc,
                    DeviceId = x.DeviceId,
                    DeviceName = x.Device.MachineName,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee != null ? x.Employee.DisplayName : null,
                    Username = x.Username,
                    ActionKey = x.ActionKey,
                    DecisionId = x.DecisionId,
                    DecisionName = x.Decision.Name,
                    DecisionDisplayName = x.Decision.DisplayName,
                    ReasonCodeId = x.ReasonCodeId,
                    ReasonCodeDisplayName = x.ReasonCode != null ? x.ReasonCode.DisplayName : null,
                    PolicyVersion = x.PolicyVersion
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<AuditEventListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<AuditEventListItemDto>>.SuccessResponse(result);
        }
    }
}
