using DLPManagementSystem.DTO.Lookups;
using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class LookupsService : ILookupsService
    {
        private readonly DLPSystemContext _db;

        public LookupsService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<List<RoleLookupDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Roles
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayName)
                .Select(x => new RoleLookupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayName = x.DisplayName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<LookupItemDto>> GetAlertLevelsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.AlertLevels
                .AsNoTracking()
                .OrderBy(x => x.MinRiskScore)
                .Select(x => new LookupItemDto { Id = x.Id, Name = x.Name })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<LookupItemDto>> GetAlertStatusesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.AlertStatuses
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto { Id = x.Id, Name = x.Name })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<LookupItemDto>> GetDeviceStatusesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.DeviceStatuses
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .Select(x => new LookupItemDto { Id = x.Id, Name = x.DisplayName })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<LookupItemDto>> GetEmployeeStatusesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.EmployeeStatuses
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .Select(x => new LookupItemDto { Id = x.Id, Name = x.DisplayName })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<LookupItemDto>> GetUserStatusesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.UserStatuses
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .Select(x => new LookupItemDto { Id = x.Id, Name = x.DisplayName })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DepartmentLookupDto>> GetDepartmentsAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            return await _db.Departments
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new DepartmentLookupDto { Id = x.Id, Name = x.Name })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<PermissionActionDto>> GetPermissionActionsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.PermissionActions
                .AsNoTracking()
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.SortOrder)
                .Select(x => new PermissionActionDto
                {
                    Key = x.Key,
                    Category = x.Category.Name,
                    DisplayName = x.DisplayName,
                    DefaultDecision = x.DefaultDecision.Name,
                    SupportsPermanentGrant = x.SupportsPermanentGrant,
                    SupportsTemporaryGrant = x.SupportsTemporaryGrant,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<LookupItemDto>> GetAuditDecisionsAsync(CancellationToken cancellationToken = default)
        {
            return await _db.AuditDecisions
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .Select(x => new LookupItemDto { Id = x.Id, Name = x.DisplayName })
                .ToListAsync(cancellationToken);
        }
    }
}
