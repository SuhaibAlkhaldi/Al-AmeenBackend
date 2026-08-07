using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Employees;

namespace DLPManagementSystem.Service.Interface
{
    public interface IEmployeeService
    {
        Task<ApiResponse<PagedResultDto<EmployeeListItemDto>>> GetEmployeesAsync(
            Guid organizationId,
            string? search,
            Guid? departmentId,
            int? statusId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<EmployeeDetailDto>> GetEmployeeByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

        Task<ApiResponse<EmployeeDetailDto>> CreateEmployeeAsync(Guid organizationId, Guid callerUserId, CreateEmployeeDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<EmployeeDetailDto>> UpdateEmployeeAsync(Guid organizationId, Guid id, Guid callerUserId, UpdateEmployeeDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> DeleteEmployeeAsync(Guid organizationId, Guid id, Guid callerUserId, CancellationToken cancellationToken = default);

        Task<ApiResponse<List<EmployeeWindowsIdentityDto>>> GetWindowsIdentitiesAsync(
            Guid organizationId, Guid employeeId, CancellationToken cancellationToken = default);

        Task<ApiResponse<EmployeeWindowsIdentityDto>> AddWindowsIdentityAsync(
            Guid organizationId, Guid employeeId, Guid callerUserId, CreateEmployeeWindowsIdentityDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> RevokeWindowsIdentityAsync(
            Guid organizationId, Guid employeeId, Guid identityId, Guid callerUserId, CancellationToken cancellationToken = default);
    }
}
