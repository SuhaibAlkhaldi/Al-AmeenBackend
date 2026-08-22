using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Devices;
using DLPManagementSystem.DTO.Users;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class UserService : IUserService
    {
        private readonly DLPSystemContext _db;
        private readonly IAdminAuditLogService _adminAuditLogService;
        private readonly IPasswordService _passwordService;
        private readonly IDeviceService _deviceService;
        private readonly IPermissionGrantService _permissionGrantService;
        private readonly IPermissionLookupService _permissionLookupService;

        public UserService(
            DLPSystemContext db,
            IAdminAuditLogService adminAuditLogService,
            IPasswordService passwordService,
            IDeviceService deviceService,
            IPermissionGrantService permissionGrantService,
            IPermissionLookupService permissionLookupService)
        {
            _db = db;
            _adminAuditLogService = adminAuditLogService;
            _passwordService = passwordService;
            _deviceService = deviceService;
            _permissionGrantService = permissionGrantService;
            _permissionLookupService = permissionLookupService;
        }

        public async Task<ApiResponse<PagedResultDto<UserListItemDto>>> GetUsersAsync(
            Guid organizationId,
            string? search,
            int? roleId,
            int? statusId,
            int? userTypeId,
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

            // Lets a caller ask for only Admin-type (or only Employee-type) accounts - e.g. the portal's
            // "Admin" filter uses this to exclude the hidden Employee-linked login accounts that would
            // otherwise show up here too (every Employee has a User row, see Employee.UserId).
            if (userTypeId.HasValue)
            {
                query = query.Where(x => x.UserTypeId == userTypeId.Value);
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

        public async Task<ApiResponse<UserDetailDto>> CreateUserAsync(Guid organizationId, Guid callerUserId, string callerRoleName, CreateUserDto request, CancellationToken cancellationToken = default)
        {
            var elevationCheck = await CheckElevationAttemptAsync(callerRoleName, request.RoleId, cancellationToken);
            if (elevationCheck != null)
            {
                return elevationCheck;
            }

            if (request.Password.Length < PasswordPolicy.MinLength)
            {
                return ApiResponse<UserDetailDto>.FailureResponse(PasswordPolicy.MessageEn, PasswordPolicy.MessageAr);
            }

            var emailExists = await _db.Users
                .AnyAsync(x => x.OrganizationId == organizationId && x.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                return ApiResponse<UserDetailDto>.FailureResponse(
                    "A user with this email already exists.",
                    "يوجد مستخدم بنفس البريد الإلكتروني");
            }

            // Every account gets its own accompanying Employee row now (see the reverse direction in
            // EmployeeService.CreateEmployeeAsync) - Employee.Email has its own separate
            // unique-per-organization index, so it must be checked here too, not just Users.Email above.
            var employeeEmailExists = await _db.Employees
                .AnyAsync(x => x.OrganizationId == organizationId && x.Email == request.Email, cancellationToken);

            if (employeeEmailExists)
            {
                return ApiResponse<UserDetailDto>.FailureResponse(
                    "An employee with this email already exists.",
                    "يوجد موظف بنفس البريد الإلكتروني");
            }

            var activeStatus = await _db.UserStatuses.FirstOrDefaultAsync(x => x.Name == "Active", cancellationToken);
            if (activeStatus == null)
            {
                return ApiResponse<UserDetailDto>.FailureResponse("Active user status is not configured.", "حالة المستخدم النشط غير مهيأة");
            }

            var activeEmployeeStatus = await _db.EmployeeStatuses.FirstOrDefaultAsync(x => x.Name == "Active", cancellationToken);
            if (activeEmployeeStatus == null)
            {
                return ApiResponse<UserDetailDto>.FailureResponse("Active employee status is not configured.", "حالة الموظف النشط غير مهيأة");
            }

            // Optional here, unlike CreateEmployeeAsync (where a device is mandatory) - validated the
            // same way when present: must exist, belong to this organization, and be Active.
            Device? device = null;
            if (request.DeviceId.HasValue && request.DeviceId.Value != Guid.Empty)
            {
                device = await _db.Devices
                    .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == request.DeviceId.Value, cancellationToken);

                if (device == null)
                {
                    return ApiResponse<UserDetailDto>.FailureResponse("Device was not found.", "الجهاز غير موجود");
                }

                var deviceActiveStatus = await _db.DeviceStatuses.FirstOrDefaultAsync(x => x.Name == "Active", cancellationToken);
                if (deviceActiveStatus == null || device.StatusId != deviceActiveStatus.Id)
                {
                    return ApiResponse<UserDetailDto>.FailureResponse(
                        "The selected device is not active.",
                        "الجهاز المحدد غير نشط");
                }
            }

            // "Suggested" is a purely frontend curation concept (which checkboxes to show for which
            // role) - the backend only ever guards the real invariant, that every key submitted is a
            // genuine, currently-enabled permission action.
            var suggestedActionKeys = (request.SuggestedPermissionActionKeys ?? new List<string>())
                .Distinct()
                .ToList();

            if (suggestedActionKeys.Count > 0)
            {
                var validActionKeys = await _db.PermissionActions
                    .Where(x => suggestedActionKeys.Contains(x.Key) && x.IsEnabled)
                    .Select(x => x.Key)
                    .ToListAsync(cancellationToken);

                var invalidActionKeys = suggestedActionKeys.Except(validActionKeys).ToList();
                if (invalidActionKeys.Count > 0)
                {
                    return ApiResponse<UserDetailDto>.FailureResponse(
                        $"Unknown or disabled permission action(s): {string.Join(", ", invalidActionKeys)}.",
                        $"إجراء(ات) صلاحية غير معروفة أو معطّلة: {string.Join(", ", invalidActionKeys)}");
                }
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = string.Empty,
                RoleId = request.RoleId,
                UserTypeId = request.UserTypeId,
                StatusId = activeStatus.Id,
                IsEmailVerified = false,
                // request.Password is mandatory here (unlike Employee creation, there's no
                // generate-if-omitted path needed) - still force a change on first sign-in, since the
                // caller who typed it is an admin, not the account holder. Relies on User's own
                // property default (true); listed explicitly since this is exactly the behavior the
                // account-creation security review asked to confirm.
                CreatedAtUtc = nowUtc
            };
            user.PasswordHash = _passwordService.HashPassword(user, request.Password);

            _db.Users.Add(user);

            // Every account - Admin or Employee user type alike - gets its own Employee row now, so
            // device assignment and permission grants (both keyed on Employee, not User - see
            // Models/DeviceUserAssignment.cs and PermissionGrant.SubjectId) work identically
            // regardless of account type. EmployeeNumber/DisplayName/Email/Status default sensibly
            // for an Admin-type account that will never actually be managed from the Employee tab;
            // DepartmentId stays null since Admin accounts aren't assigned to a company department here.
            var employeeNumber = await TryGenerateUniqueEmployeeNumberAsync(organizationId, cancellationToken);

            if (employeeNumber == null)
            {
                return ApiResponse<UserDetailDto>.FailureResponse(
                    "Could not generate a unique employee number. Please try again.",
                    "تعذر إنشاء رقم وظيفي فريد، يرجى المحاولة مرة أخرى");
            }

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = user.Id,
                DepartmentId = null,
                EmployeeNumber = employeeNumber,
                DisplayName = request.FullName,
                Email = request.Email,
                StatusId = activeEmployeeStatus.Id,
                CreatedAtUtc = nowUtc
            };

            _db.Employees.Add(employee);

            if (suggestedActionKeys.Count > 0)
            {
                int decisionId;
                int subjectTypeId;

                try
                {
                    decisionId = await _permissionLookupService.GetPermissionDecisionId("Allow", cancellationToken);
                    subjectTypeId = await _permissionLookupService.GetPermissionSubjectTypeId("Employee", cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    return ApiResponse<UserDetailDto>.FailureResponse("Required reference data was not found.", "بيانات مرجعية مطلوبة غير موجودة");
                }

                var targetRole = await _db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
                var roleLabel = targetRole?.DisplayName ?? "this role";

                foreach (var actionKey in suggestedActionKeys)
                {
                    var buildResult = await _permissionGrantService.BuildGrantAsync(
                        organizationId,
                        actionKey,
                        decisionId,
                        subjectTypeId,
                        employee.Id.ToString(),
                        targetDeviceId: null,
                        grantTypeName: "Permanent",
                        requestedStartsAtUtc: null,
                        requestedExpiresAtUtc: null,
                        reason: $"Suggested default permission for the {roleLabel} role, selected at account creation.",
                        grantedByUserId: callerUserId,
                        sourcePermissionRequestId: null,
                        cancellationToken);

                    if (!buildResult.Success)
                    {
                        return ApiResponse<UserDetailDto>.FailureResponse(buildResult.ErrorMessageEn!, buildResult.ErrorMessageAr!);
                    }
                }
            }

            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "UserCreated", "User", user.Id, user.FullName,
                $"Created user with role id {request.RoleId} and user type id {request.UserTypeId}.", cancellationToken);

            // Transaction-wrapped for the same reason as EmployeeService.CreateEmployeeAsync: without
            // it, a failure inside AssignDeviceAsync's own separate SaveChangesAsync call (e.g. two
            // concurrent creates racing over PolicyVersionService's version-number generation for the
            // same device) left the User+Employee(+grants) row(s) already committed - an orphaned
            // account the caller was told never got created. Found live during this feature's
            // concurrency audit.
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);

                // Same ordering constraint as EmployeeService.CreateEmployeeAsync: AssignDeviceAsync
                // looks the employee up via a fresh database query, so it must run after the save
                // above, as its own follow-up step rather than being folded into it.
                if (device != null)
                {
                    var assignResult = await _deviceService.AssignDeviceAsync(
                        organizationId,
                        device.Id,
                        callerUserId,
                        new AssignDeviceDto { EmployeeId = employee.Id },
                        cancellationToken);

                    if (!assignResult.Success)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return ApiResponse<UserDetailDto>.FailureResponse(assignResult.MessageEn, assignResult.MessageAr);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

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

            var oldRoleId = user.RoleId;
            var roleChanged = oldRoleId != request.RoleId;

            user.FullName = request.FullName;
            user.RoleId = request.RoleId;
            user.StatusId = request.StatusId;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;

            if (roleChanged)
            {
                await _adminAuditLogService.LogAsync(
                    organizationId, callerUserId, "UserRoleChanged", "User", user.Id, user.FullName,
                    $"Role changed from role id {oldRoleId} to role id {request.RoleId}.", cancellationToken);
            }
            else
            {
                await _adminAuditLogService.LogAsync(
                    organizationId, callerUserId, "UserUpdated", "User", user.Id, user.FullName,
                    $"Status set to status id {request.StatusId}.", cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            return await GetUserByIdAsync(organizationId, id, cancellationToken);
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(Guid organizationId, Guid id, Guid callerUserId, ResetPasswordDto request, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (user == null)
            {
                return ApiResponse<bool>.FailureResponse("User was not found.", "المستخدم غير موجود");
            }

            if (request.NewPassword.Length < PasswordPolicy.MinLength)
            {
                return ApiResponse<bool>.FailureResponse(PasswordPolicy.MessageEn, PasswordPolicy.MessageAr);
            }

            user.PasswordHash = _passwordService.HashPassword(user, request.NewPassword);
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;
            // The account holder didn't choose this password - force them to pick their own on next
            // sign-in, same as a freshly created account.
            user.MustChangePassword = true;

            // Never log the actual password value here - only that a reset happened.
            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "PasswordReset", "User", user.Id, user.FullName,
                null, cancellationToken);

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

            await _adminAuditLogService.LogAsync(
                organizationId, callerUserId, "UserDeleted", "User", user.Id, user.FullName,
                null, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "User disabled successfully.", "تم تعطيل المستخدم بنجاح");
        }

        // Which roles a caller in a given role is allowed to hand to someone else, on both create and
        // edit. A caller role with no entry here is unrestricted (SuperAdmin today) - the map only
        // ever narrows what a role can assign, so a role nobody has explicitly restricted defaults to
        // "no restriction" rather than "locked out", and adding a new restricted role later is a
        // one-line addition here, not a new branch of hand-written logic.
        private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AssignableRoleNamesByCallerRole =
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // HelpDesk can create/edit accounts but must never be able to hand out SuperAdmin/
                // SecurityAdmin privileges it doesn't itself have.
                ["HelpDesk"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HelpDesk", "Auditor", "Employee" }
            };

        private async Task<ApiResponse<UserDetailDto>?> CheckElevationAttemptAsync(string callerRoleName, int targetRoleId, CancellationToken cancellationToken)
        {
            if (!AssignableRoleNamesByCallerRole.TryGetValue(callerRoleName, out var assignableRoleNames))
            {
                return null;
            }

            var targetRole = await _db.Roles.FirstOrDefaultAsync(x => x.Id == targetRoleId, cancellationToken);

            if (targetRole != null && !assignableRoleNames.Contains(targetRole.Name))
            {
                return ApiResponse<UserDetailDto>.FailureResponse(
                    $"{callerRoleName} accounts are not permitted to assign the {targetRole.DisplayName} role.",
                    $"حسابات {callerRoleName} غير مخوّلة لمنح دور {targetRole.DisplayName}");
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
