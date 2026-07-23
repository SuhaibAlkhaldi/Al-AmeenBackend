using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Users;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class UserService : IUserService
    {
        private readonly DLPSystemContext _db;

        public UserService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<PagedResultDto<UserListItemDto>>> GetUsersAsync(
            Guid organizationId,
            string? search,
            int? roleId,
            int? statusId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Users
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.FullName.Contains(search) || x.Email.Contains(search));
            }

            if (roleId.HasValue)
            {
                query = query.Where(x => x.RoleId == roleId.Value);
            }

            if (statusId.HasValue)
            {
                query = query.Where(x => x.StatusId == statusId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new UserListItemDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    RoleId = x.RoleId,
                    RoleName = x.Role.Name,
                    StatusId = x.StatusId,
                    StatusName = x.Status.Name,
                    LastLoginAtUtc = x.LastLoginAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<UserListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<UserListItemDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Include(x => x.Role)
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (user == null)
            {
                return ApiResponse<UserDetailDto>.FailureResponse("User was not found.", "المستخدم غير موجود");
            }

            var employee = await _db.Employees
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.UserId == id)
                .Select(x => new { x.Id, x.DisplayName })
                .FirstOrDefaultAsync(cancellationToken);

            var dto = MapToDetail(user, employee?.Id, employee?.DisplayName);

            return ApiResponse<UserDetailDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<UserDetailDto>> CreateUserAsync(Guid organizationId, string callerRoleName, CreateUserDto request, CancellationToken cancellationToken = default)
        {
            var elevationCheck = await CheckElevationAttemptAsync(callerRoleName, request.RoleId, cancellationToken);
            if (elevationCheck != null)
            {
                return elevationCheck;
            }

            var emailExists = await _db.Users
                .AnyAsync(x => x.OrganizationId == organizationId && x.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                return ApiResponse<UserDetailDto>.FailureResponse(
                    "A user with this email already exists.",
                    "يوجد مستخدم بنفس البريد الإلكتروني");
            }

            var activeStatus = await _db.UserStatuses.FirstOrDefaultAsync(x => x.Name == "Active", cancellationToken);
            if (activeStatus == null)
            {
                return ApiResponse<UserDetailDto>.FailureResponse("Active user status is not configured.", "حالة المستخدم النشط غير مهيأة");
            }

            // A User created with the "Employee" user type needs a linked Employee row, mirroring
            // EmployeeService.CreateEmployeeAsync's reverse direction — otherwise the account can never
            // submit a permission request (PermissionRequestService.CreateAsync requires one).
            var employeeUserType = await _db.UserTypes.FirstOrDefaultAsync(x => x.Name == "Employee", cancellationToken);
            var isEmployeeUserType = employeeUserType != null && request.UserTypeId == employeeUserType.Id;

            EmployeeStatus? activeEmployeeStatus = null;

            if (isEmployeeUserType)
            {
                activeEmployeeStatus = await _db.EmployeeStatuses.FirstOrDefaultAsync(x => x.Name == "Active", cancellationToken);
                if (activeEmployeeStatus == null)
                {
                    return ApiResponse<UserDetailDto>.FailureResponse("Active employee status is not configured.", "حالة الموظف النشط غير مهيأة");
                }
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = SecurityHashHelper.Sha256(request.Password),
                RoleId = request.RoleId,
                UserTypeId = request.UserTypeId,
                StatusId = activeStatus.Id,
                IsEmailVerified = false,
                CreatedAtUtc = nowUtc
            };

            _db.Users.Add(user);

            if (isEmployeeUserType)
            {
                var employeeNumber = await TryGenerateUniqueEmployeeNumberAsync(organizationId, cancellationToken);

                if (employeeNumber == null)
                {
                    return ApiResponse<UserDetailDto>.FailureResponse(
                        "Could not generate a unique employee number. Please try again.",
                        "تعذر إنشاء رقم وظيفي فريد، يرجى المحاولة مرة أخرى");
                }

                _db.Employees.Add(new Employee
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    UserId = user.Id,
                    DepartmentId = null,
                    EmployeeNumber = employeeNumber,
                    DisplayName = request.FullName,
                    Email = request.Email,
                    StatusId = activeEmployeeStatus!.Id,
                    CreatedAtUtc = nowUtc
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetUserByIdAsync(organizationId, user.Id, cancellationToken);
        }

        private async Task<string?> TryGenerateUniqueEmployeeNumberAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var candidate = $"USR-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

                var exists = await _db.Employees
                    .AnyAsync(x => x.OrganizationId == organizationId && x.EmployeeNumber == candidate, cancellationToken);

                if (!exists)
                {
                    return candidate;
                }
            }

            return null;
        }

        public async Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid organizationId, Guid id, Guid callerUserId, string callerRoleName, UpdateUserDto request, CancellationToken cancellationToken = default)
        {
            var elevationCheck = await CheckElevationAttemptAsync(callerRoleName, request.RoleId, cancellationToken);
            if (elevationCheck != null)
            {
                return elevationCheck;
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (user == null)
            {
                return ApiResponse<UserDetailDto>.FailureResponse("User was not found.", "المستخدم غير موجود");
            }

            if (id == callerUserId)
            {
                var targetStatus = await _db.UserStatuses.FirstOrDefaultAsync(x => x.Id == request.StatusId, cancellationToken);
                if (targetStatus != null && !string.Equals(targetStatus.Name, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<UserDetailDto>.FailureResponse(
                        "You cannot disable or deactivate your own account.",
                        "لا يمكنك تعطيل أو إلغاء تفعيل حسابك الخاص");
                }
            }

            user.FullName = request.FullName;
            user.RoleId = request.RoleId;
            user.StatusId = request.StatusId;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return await GetUserByIdAsync(organizationId, id, cancellationToken);
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(Guid organizationId, Guid id, ResetPasswordDto request, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (user == null)
            {
                return ApiResponse<bool>.FailureResponse("User was not found.", "المستخدم غير موجود");
            }

            user.PasswordHash = SecurityHashHelper.Sha256(request.NewPassword);
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Password reset successfully.", "تم إعادة تعيين كلمة المرور بنجاح");
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(Guid organizationId, Guid id, Guid callerUserId, CancellationToken cancellationToken = default)
        {
            if (id == callerUserId)
            {
                return ApiResponse<bool>.FailureResponse(
                    "You cannot disable your own account.",
                    "لا يمكنك تعطيل حسابك الخاص");
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (user == null)
            {
                return ApiResponse<bool>.FailureResponse("User was not found.", "المستخدم غير موجود");
            }

            var disabledStatus = await _db.UserStatuses.FirstOrDefaultAsync(x => x.Name == "Disabled", cancellationToken);
            if (disabledStatus == null)
            {
                return ApiResponse<bool>.FailureResponse("Disabled user status is not configured.", "حالة المستخدم المعطل غير مهيأة");
            }

            user.StatusId = disabledStatus.Id;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "User disabled successfully.", "تم تعطيل المستخدم بنجاح");
        }

        private async Task<ApiResponse<UserDetailDto>?> CheckElevationAttemptAsync(string callerRoleName, int targetRoleId, CancellationToken cancellationToken)
        {
            // HelpDesk can create/edit accounts but must never be able to hand out SuperAdmin/SecurityAdmin
            // privileges it doesn't itself have — SuperAdmin remains unrestricted and can assign any role.
            if (!string.Equals(callerRoleName, "HelpDesk", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var targetRole = await _db.Roles.FirstOrDefaultAsync(x => x.Id == targetRoleId, cancellationToken);

            if (targetRole != null &&
                (string.Equals(targetRole.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(targetRole.Name, "SecurityAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                return ApiResponse<UserDetailDto>.FailureResponse(
                    "HelpDesk accounts are not permitted to assign the SuperAdmin or SecurityAdmin role.",
                    "حسابات الدعم الفني غير مخوّلة لمنح صلاحية المسؤول الأعلى أو مسؤول الأمان");
            }

            return null;
        }

        private static UserDetailDto MapToDetail(User user, Guid? employeeId, string? employeeName)
        {
            return new UserDetailDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role.Name,
                StatusId = user.StatusId,
                StatusName = user.Status.Name,
                LastLoginAtUtc = user.LastLoginAtUtc,
                CreatedAtUtc = user.CreatedAtUtc,
                EmployeeId = employeeId,
                EmployeeName = employeeName
            };
        }
    }
}
