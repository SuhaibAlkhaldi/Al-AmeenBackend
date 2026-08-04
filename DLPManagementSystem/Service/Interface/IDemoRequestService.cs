using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.DemoRequests;

namespace DLPManagementSystem.Service.Interface
{
    public interface IDemoRequestService
    {
        Task<ApiResponse<DemoRequestListItemDto>> CreateAsync(
            CreateDemoRequestDto request, string? sourceIp, CancellationToken cancellationToken = default);

        Task<ApiResponse<PagedResultDto<DemoRequestListItemDto>>> GetListAsync(
            int? statusId, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<ApiResponse<DemoRequestListItemDto>> UpdateStatusAsync(
            Guid id, UpdateDemoRequestStatusDto request, CancellationToken cancellationToken = default);
    }
}
