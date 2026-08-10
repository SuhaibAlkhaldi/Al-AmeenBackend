using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Devices;

namespace DLPManagementSystem.Service.Interface
{
    public interface IDeviceService
    {
        Task<ApiResponse<PagedResultDto<DeviceListItemDto>>> GetDevicesAsync(
            Guid organizationId,
            string? search,
            int? statusId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<DeviceDetailDto>> GetDeviceByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

        Task<ApiResponse<DeviceDetailDto>> UpdateDeviceAsync(Guid organizationId, Guid id, Guid callerUserId, UpdateDeviceDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> DeleteDeviceAsync(Guid organizationId, Guid id, Guid callerUserId, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> AssignDeviceAsync(Guid organizationId, Guid id, Guid assignedByUserId, AssignDeviceDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> UnassignDeviceAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

        Task<ApiResponse<List<DeviceAssignmentDto>>> GetDeviceAssignmentsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> AddDeviceAssignmentAsync(Guid organizationId, Guid id, Guid assignedByUserId, AssignDeviceDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> RemoveDeviceAssignmentAsync(Guid organizationId, Guid id, Guid employeeId, CancellationToken cancellationToken = default);
    }
}
