using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileInventory;
using Microsoft.AspNetCore.Http;

namespace DLPManagementSystem.Service.Interface
{
    public interface IFileInventoryReportService
    {
        Task<ApiResponse<PagedResultDto<FileInventoryReportItemDto>>> GetFileInventoryAsync(
            Guid organizationId,
            List<Guid>? employeeIds,
            List<string>? classifications,
            List<string>? provenances,
            List<int>? fileTypeIds,
            bool? isProtected,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task ExportFileInventoryAsync(
            Guid organizationId,
            List<Guid>? employeeIds,
            List<string>? classifications,
            List<string>? provenances,
            List<int>? fileTypeIds,
            bool? isProtected,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            HttpResponse response,
            CancellationToken cancellationToken = default);
    }
}
