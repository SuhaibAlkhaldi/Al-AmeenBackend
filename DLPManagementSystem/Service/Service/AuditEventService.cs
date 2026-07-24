using DLPManagementSystem.Common;
using DLPManagementSystem.CompanyDlpDashboard;
using DLPManagementSystem.DTO.AuditEvents;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class AuditEventService : IAuditEventService
    {
        private const int MaxExportRows = 50_000;

        private readonly DLPSystemContext _db;

        public AuditEventService(DLPSystemContext db)
        {
            _db = db;
        }

        private IQueryable<AuditEvent> BuildFilteredQuery(
            Guid organizationId,
            Guid? deviceId,
            Guid? employeeId,
            string? actionKey,
            int? decisionId,
            int? reasonCodeId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc)
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

            return query;
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
            var query = BuildFilteredQuery(organizationId, deviceId, employeeId, actionKey, decisionId, reasonCodeId, fromUtc, toUtc);

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
                    PolicyVersion = x.PolicyVersion,
                    Details = x.MetadataJson
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.Details = DlpMetadataDetailsBuilder.Build(item.Details);
            }

            var result = new PagedResultDto<AuditEventListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<AuditEventListItemDto>>.SuccessResponse(result);
        }

        public async Task ExportAuditEventsAsync(
            Guid organizationId,
            Guid? deviceId,
            Guid? employeeId,
            string? actionKey,
            int? decisionId,
            int? reasonCodeId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            HttpResponse response,
            CancellationToken cancellationToken = default)
        {
            var query = BuildFilteredQuery(organizationId, deviceId, employeeId, actionKey, decisionId, reasonCodeId, fromUtc, toUtc);

            var totalMatching = await query.CountAsync(cancellationToken);
            var truncated = totalMatching > MaxExportRows;

            var fileName = $"audit-events-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv";
            response.ContentType = "text/csv";
            response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            if (truncated)
            {
                response.Headers["X-Export-Truncated"] = "true";
            }

            await using var writer = new StreamWriter(response.Body, leaveOpen: true);
            writer.NewLine = "\r\n";

            await writer.WriteLineAsync(CsvWriterHelper.BuildRow(
                "OccurredAtUtc", "ReceivedAtUtc", "Device", "Employee/User", "ActionKey", "Decision", "Reason", "PolicyVersion", "Details"));

            var rows = query
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(MaxExportRows)
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
                    PolicyVersion = x.PolicyVersion,
                    Details = x.MetadataJson
                })
                .AsAsyncEnumerable();

            await foreach (var item in rows.WithCancellation(cancellationToken))
            {
                var row = CsvWriterHelper.BuildRow(
                    item.OccurredAtUtc.ToString("o"),
                    item.ReceivedAtUtc.ToString("o"),
                    item.DeviceName,
                    item.EmployeeName ?? item.Username ?? string.Empty,
                    item.ActionKey,
                    item.DecisionDisplayName,
                    item.ReasonCodeDisplayName ?? string.Empty,
                    item.PolicyVersion?.ToString() ?? string.Empty,
                    DlpMetadataDetailsBuilder.Build(item.Details) ?? string.Empty);

                await writer.WriteLineAsync(row);
            }

            await writer.FlushAsync(cancellationToken);
        }
    }
}
