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

        Task<ApiResponse<EmployeeDetailDto>> CreateEmployeeAsync(Guid organizationId, CreateEmployeeDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<EmployeeDetailDto>> UpdateEmployeeAsync(Guid organizationId, Guid id, UpdateEmployeeDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> DeleteEmployeeAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
    }
}
