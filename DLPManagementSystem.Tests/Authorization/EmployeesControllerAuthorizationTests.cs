using System.Linq;
using System.Reflection;
using DLPManagementSystem.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace DLPManagementSystem.Tests.Authorization
{
    // Guards the ui-data-batch1 addition of EmployeeListItemDto.LastLoginAtUtc: that field is only
    // safe to expose because GetEmployees/GetEmployeeById are already admin-only. This asserts the
    // [Authorize] attribute enforcing that never silently regresses (e.g. someone widening the
    // roles list, or adding a looser override on one action) to include the "Employee" role -
    // which would let a regular employee read another employee's (or anyone's) last-login time.
    public class EmployeesControllerAuthorizationTests
    {
        [Fact]
        public void ControllerClass_AuthorizeAttribute_DoesNotIncludeEmployeeRole()
        {
            var attribute = typeof(EmployeesController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attribute);
            Assert.NotNull(attribute!.Roles);
            var roles = attribute.Roles!.Split(',').Select(r => r.Trim());
            Assert.DoesNotContain("Employee", roles);
        }

        [Theory]
        [InlineData(nameof(EmployeesController.GetEmployees))]
        [InlineData(nameof(EmployeesController.GetEmployeeById))]
        public void ListAndDetailActions_HaveNoActionLevelOverrideWideningAccess(string methodName)
        {
            var method = typeof(EmployeesController).GetMethods()
                .First(m => m.Name == methodName);

            // Neither action may carry its own [Authorize] (which would replace, not add to, the
            // class-level one) or [AllowAnonymous] - both would bypass the admin-only gate the
            // class-level attribute provides today.
            Assert.Null(method.GetCustomAttribute<AuthorizeAttribute>());
            Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        }
    }
}
