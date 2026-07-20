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

        public EmployeeService(DLPSystemContext db)
        {
            _db = db;
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

        public async Task<ApiResponse<EmployeeDetailDto>> CreateEmployeeAsync(Guid organizationId, CreateEmployeeDto request, CancellationToken cancellationToken = default)
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

            await _db.SaveChangesAsync(cancellationToken);

            return await GetEmployeeByIdAsync(organizationId, employee.Id, cancellationToken);
        }

        public async Task<ApiResponse<EmployeeDetailDto>> UpdateEmployeeAsync(Guid organizationId, Guid id, UpdateEmployeeDto request, CancellationToken cancellationToken = default)
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

            await _db.SaveChangesAsync(cancellationToken);

            return await GetEmployeeByIdAsync(organizationId, id, cancellationToken);
        }

        public async Task<ApiResponse<bool>> DeleteEmployeeAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
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

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Employee terminated successfully.", "تم إنهاء خدمة الموظف بنجاح");
        }
    }
}
