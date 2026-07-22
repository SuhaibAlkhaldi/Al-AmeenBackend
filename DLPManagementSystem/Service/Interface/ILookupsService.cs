using DLPManagementSystem.DTO.Lookups;
using DLPManagementSystem.DTO.Permissions.Contracts;

namespace DLPManagementSystem.Service.Interface
{
    public interface ILookupsService
    {
        Task<List<RoleLookupDto>> GetRolesAsync(CancellationToken cancellationToken = default);
        Task<List<LookupItemDto>> GetAlertLevelsAsync(CancellationToken cancellationToken = default);
        Task<List<LookupItemDto>> GetAlertStatusesAsync(CancellationToken cancellationToken = default);
        Task<List<LookupItemDto>> GetDeviceStatusesAsync(CancellationToken cancellationToken = default);
        Task<List<LookupItemDto>> GetEmployeeStatusesAsync(CancellationToken cancellationToken = default);
        Task<List<LookupItemDto>> GetUserStatusesAsync(CancellationToken cancellationToken = default);
        Task<List<DepartmentLookupDto>> GetDepartmentsAsync(Guid organizationId, CancellationToken cancellationToken = default);
        Task<List<PermissionActionDto>> GetPermissionActionsAsync(CancellationToken cancellationToken = default);
        Task<List<LookupItemDto>> GetAuditDecisionsAsync(CancellationToken cancellationToken = default);
    }
}
