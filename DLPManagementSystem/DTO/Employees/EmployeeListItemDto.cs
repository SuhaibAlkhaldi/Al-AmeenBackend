namespace DLPManagementSystem.DTO.Employees
{
    public class EmployeeListItemDto
    {
        public Guid Id { get; set; }

        // The 1:1 login account every Employee has (see Employee.UserId) - exposed so the portal can
        // act on it directly (e.g. reset its password) without a separate lookup by name.
        public Guid UserId { get; set; }

        // The linked login account's role (normally always the fixed "Employee" role - see
        // EmployeeService.CreateEmployeeAsync - but not guaranteed for older rows created through
        // UserService.CreateUserAsync with UserTypeId=Employee and a different RoleId).
        public string RoleName { get; set; } = string.Empty;

        public Guid? DepartmentId { get; set; }

        public string? DepartmentName { get; set; }

        public string EmployeeNumber { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? JobTitle { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }

        // The linked login account's last successful sign-in (see User.LastLoginAtUtc, set by
        // AuthService on every successful login) - exposed here for the same reason RoleName is:
        // this endpoint is already admin-only (EmployeesController's class-level [Authorize] -
        // SuperAdmin/SecurityAdmin/HelpDesk/Auditor), so this adds no new exposure beyond what an
        // admin viewing this employee can already see (name, email, role).
        public DateTimeOffset? LastLoginAtUtc { get; set; }
    }
}
