using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Employees;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class EmployeeService : IEmployeeService
    {
        private readonly DLPSystemContext _db;
        private readonly IAdminAuditLogService _adminAuditLogService;

        public EmployeeService(DLPSystemContext db, IAdminAuditLogService adminAuditLogService)
        {
            _db = db;
            _adminAuditLogService = adminAuditLogService;
        }

        public async Task<ApiResponse<PagedResultDto<EmployeeListItemDto>>> GetEmployeesAsync(
            Guid organizationId,
            string? search,
            Guid? departmentId,
            int? statusId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Employees
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.DisplayName.Contains(search) || x.Email.Contains(search) || x.EmployeeNumber.Contains(search));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(x => x.DepartmentId == departmentId.Value);
            }

            if (statusId.HasValue)
            {
                query = query.Where(x => x.StatusId == statusId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.DisplayName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EmployeeListItemDto
                {
                    Id = x.Id,
                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.Department != null ? x.Department.Name : null,
                    EmployeeNumber = x.EmployeeNumber,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    StatusId = x.StatusId,
                    StatusName = x.Status.DisplayName,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<EmployeeListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<EmployeeListItemDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<EmployeeDetailDto>> GetEmployeeByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == id)
                .Select(x => new EmployeeDetailDto
                {
                    Id = x.Id,
                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.Department != null ? x.Department.Name : null,
                    EmployeeNumber = x.EmployeeNumber,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    StatusId = x.StatusId,
                    StatusName = x.Status.DisplayName,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (employee == null)
            {
                return ApiResponse<EmployeeDetailDto>.FailureResponse("Employee was not found.", "الموظف غير موجود");
            }

            return ApiResponse<EmployeeDetailDto>.SuccessResponse(employee);
        }

        public async Task<ApiResponse<EmployeeDetailDto>> CreateEmployeeAsync(Guid organizationId, Guid callerUserId, CreateEmployeeDto request, CancellationToken cancellationToken = default)
        {
            var emailExists = await _db.Employees
                .AnyAsync(x => x.OrganizationId == organizationId && x.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                return ApiResponse<EmployeeDetailDto>.FailureResponse(
                    "An employee with this email already exists.",
                    "يوجد موظف بنفس البريد الإلكتروني");
            }

            var numberExists = await _db.Employees
                .AnyAsync(x => x.OrganizationId == organizationId && x.EmployeeNumber == request.EmployeeNumber, cancellationToken);

            if (numberExists)
            {
                return ApiResponse<EmployeeDetailDto>.FailureResponse(
                    "An employee with this employee number already exists.",
                    "يوجد موظف بنفس الرقم الوظيفي");
            }

            if (request.DepartmentId.HasValue)
            {
                var departmentExists = await _db.Departments
                    .AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.DepartmentId.Value, cancellationToken);

                if (!departmentExists)
                {
                    return ApiResponse<EmployeeDetailDto>.FailureResponse("Department was not found.", "القسم غير موجود");
                }
            }

            var employeeRole = await _db.Roles.FirstOrDefaultAsync(x => x.Name == "Employee", cancellationToken);
            var employeeUserType = await _db.UserTypes.FirstOrDefaultAsync(x => x.Name == "Employee", cancellationToken);
            var activeUserStatus = await _db.UserStatuses.FirstOrDefaultAsync(x => x.Name == "Active", cancellationToken);
            var activeEmployeeStatus = await _db.EmployeeStatuses.FirstOrDefaultAsync(x => x.Name == "Active", cancellationToken);

            if (employeeRole == null || employeeUserType == null || activeUserStatus == null || activeEmployeeStatus == null)
            {
                return ApiResponse<EmployeeDetailDto>.FailureResponse(
                    "Required lookup data is missing (Employee role/user type or Active statuses).",
                    "بيانات مرجعية مطلوبة غير موجودة");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var linkedUser = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                FullName = request.DisplayName,
                Email = request.Email,
                PasswordHash = SecurityHashHelper.Sha256(SecurityHashHelper.GenerateSecret()),
                RoleId = employeeRole.Id,
                UserTypeId = employeeUserType.Id,
                StatusId = activeUserStatus.Id,
                IsEmailVerified = false,
                CreatedAtUtc = nowUtc
            };

            _db.Users.Add(linkedUser);

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = linkedUser.Id,
                DepartmentId = request.DepartmentId,
                EmployeeNumber = request.EmployeeNumber,
                DisplayName = request.DisplayName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                StatusId = activeEmployeeStatus.Id,
                CreatedAtUtc = nowUtc
            };

            _db.Employees.Add(employee);

            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "EmployeeCreated", "Employee", employee.Id, employee.DisplayName,
                $"Employee number {employee.EmployeeNumber}.", cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return await GetEmployeeByIdAsync(organizationId, employee.Id, cancellationToken);
        }

        public async Task<ApiResponse<EmployeeDetailDto>> UpdateEmployeeAsync(Guid organizationId, Guid id, Guid callerUserId, UpdateEmployeeDto request, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (employee == null)
            {
                return ApiResponse<EmployeeDetailDto>.FailureResponse("Employee was not found.", "الموظف غير موجود");
            }

            if (request.DepartmentId.HasValue)
            {
                var departmentExists = await _db.Departments
                    .AnyAsync(x => x.OrganizationId == organizationId && x.Id == request.DepartmentId.Value, cancellationToken);

                if (!departmentExists)
                {
                    return ApiResponse<EmployeeDetailDto>.FailureResponse("Department was not found.", "القسم غير موجود");
                }
            }

            employee.DepartmentId = request.DepartmentId;
            employee.EmployeeNumber = request.EmployeeNumber;
            employee.DisplayName = request.DisplayName;
            employee.Email = request.Email;
            employee.PhoneNumber = request.PhoneNumber;
            employee.StatusId = request.StatusId;
            employee.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "EmployeeUpdated", "Employee", employee.Id, employee.DisplayName,
                null, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return await GetEmployeeByIdAsync(organizationId, id, cancellationToken);
        }

        public async Task<ApiResponse<bool>> DeleteEmployeeAsync(Guid organizationId, Guid id, Guid callerUserId, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (employee == null)
            {
                return ApiResponse<bool>.FailureResponse("Employee was not found.", "الموظف غير موجود");
            }

            var terminatedStatus = await _db.EmployeeStatuses.FirstOrDefaultAsync(x => x.Name == "Terminated", cancellationToken);
            if (terminatedStatus == null)
            {
                return ApiResponse<bool>.FailureResponse("Terminated employee status is not configured.", "حالة إنهاء الخدمة غير مهيأة");
            }

            employee.StatusId = terminatedStatus.Id;
            employee.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "EmployeeDeleted", "Employee", employee.Id, employee.DisplayName,
                null, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Employee terminated successfully.", "تم إنهاء خدمة الموظف بنجاح");
        }

        public async Task<ApiResponse<List<EmployeeWindowsIdentityDto>>> GetWindowsIdentitiesAsync(
            Guid organizationId, Guid employeeId, CancellationToken cancellationToken = default)
        {
            var employeeExists = await _db.Employees
                .AnyAsync(x => x.OrganizationId == organizationId && x.Id == employeeId, cancellationToken);

            if (!employeeExists)
            {
                return ApiResponse<List<EmployeeWindowsIdentityDto>>.FailureResponse("Employee was not found.", "الموظف غير موجود");
            }

            var identities = await _db.EmployeeWindowsIdentities
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.EmployeeId == employeeId)
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Select(x => new EmployeeWindowsIdentityDto
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    DomainName = x.DomainName,
                    Username = x.Username,
                    UserSid = x.UserSid,
                    IsPrimary = x.IsPrimary,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RevokedAtUtc = x.RevokedAtUtc
                })
                .ToListAsync(cancellationToken);

            return ApiResponse<List<EmployeeWindowsIdentityDto>>.SuccessResponse(identities);
        }

        // Manual admin entry only - see CreateEmployeeWindowsIdentityDto. The unique index
        // UX_EmployeeWindowsIdentities_Org_UserSid_Active (OrganizationId+UserSid, active rows only)
        // means a SID can only ever be actively registered to one employee per organization at a time;
        // checked here explicitly so a duplicate registration gets a friendly bilingual error instead
        // of a raw DbUpdateException from the unique-index violation.
        public async Task<ApiResponse<EmployeeWindowsIdentityDto>> AddWindowsIdentityAsync(
            Guid organizationId, Guid employeeId, Guid callerUserId, CreateEmployeeWindowsIdentityDto request, CancellationToken cancellationToken = default)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == employeeId, cancellationToken);

            if (employee == null)
            {
                return ApiResponse<EmployeeWindowsIdentityDto>.FailureResponse("Employee was not found.", "الموظف غير موجود");
            }

            var sidAlreadyActive = await _db.EmployeeWindowsIdentities
                .AnyAsync(x => x.OrganizationId == organizationId && x.UserSid == request.UserSid && x.RevokedAtUtc == null, cancellationToken);

            if (sidAlreadyActive)
            {
                return ApiResponse<EmployeeWindowsIdentityDto>.FailureResponse(
                    "This Windows SID is already actively registered to an employee in this organization.",
                    "هذا الـ SID مسجَّل بالفعل لموظف آخر بهذه المؤسسة");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var identity = new EmployeeWindowsIdentity
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                EmployeeId = employeeId,
                DomainName = request.DomainName,
                Username = request.Username,
                UserSid = request.UserSid,
                IsPrimary = request.IsPrimary,
                CreatedAtUtc = nowUtc
            };

            _db.EmployeeWindowsIdentities.Add(identity);

            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "EmployeeWindowsIdentityAdded", "Employee", employeeId, employee.DisplayName,
                $"Registered Windows identity '{request.Username}' ({request.UserSid}).", cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<EmployeeWindowsIdentityDto>.SuccessResponse(new EmployeeWindowsIdentityDto
            {
                Id = identity.Id,
                EmployeeId = identity.EmployeeId,
                DomainName = identity.DomainName,
                Username = identity.Username,
                UserSid = identity.UserSid,
                IsPrimary = identity.IsPrimary,
                CreatedAtUtc = identity.CreatedAtUtc,
                RevokedAtUtc = identity.RevokedAtUtc
            }, "Windows identity registered successfully.", "تم تسجيل الهوية بنجاح");
        }

        public async Task<ApiResponse<bool>> RevokeWindowsIdentityAsync(
            Guid organizationId, Guid employeeId, Guid identityId, Guid callerUserId, CancellationToken cancellationToken = default)
        {
            var identity = await _db.EmployeeWindowsIdentities
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.EmployeeId == employeeId && x.Id == identityId, cancellationToken);

            if (identity == null)
            {
                return ApiResponse<bool>.FailureResponse("Windows identity was not found.", "الهوية غير موجودة");
            }

            if (identity.RevokedAtUtc != null)
            {
                return ApiResponse<bool>.FailureResponse("Windows identity is already revoked.", "الهوية مُلغاة بالفعل");
            }

            identity.RevokedAtUtc = DateTimeOffset.UtcNow;

            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "EmployeeWindowsIdentityRevoked", "Employee", employeeId, null,
                $"Revoked Windows identity '{identity.Username}' ({identity.UserSid}).", cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Windows identity revoked successfully.", "تم إلغاء الهوية بنجاح");
        }
    }
}
