using System.Reflection;
using DLPManagementSystem.CompanyDlpDashboard;
using Microsoft.AspNetCore.Authorization;

namespace DLPManagementSystem.Tests.Authorization
{
    // dashboard-split-endpoints: the six new split endpoints must be reachable by exactly the same
    // callers as the existing GetSummary action - same class-level [Authorize] (any authenticated
    // user, no role restriction), no per-action override. Same reflection-based convention as
    // EmployeesControllerAuthorizationTests, since this project has no WebApplicationFactory-based
    // integration tests to assert a live 401/403 against.
    public class DlpDashboardControllerAuthorizationTests
    {
        private static readonly string[] AllActionNames =
        {
            nameof(DlpDashboardController.GetSummary),
            nameof(DlpDashboardController.GetKpis),
            nameof(DlpDashboardController.GetStatus),
            nameof(DlpDashboardController.GetRecentEvents),
            nameof(DlpDashboardController.GetBreakdown),
            nameof(DlpDashboardController.GetRisk),
            nameof(DlpDashboardController.GetWeeklyTrend)
        };

        [Fact]
        public void ControllerClass_HasAuthorizeWithNoRoleRestriction()
        {
            var attribute = typeof(DlpDashboardController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(attribute);
            // Same as today's summary-only controller: open to any authenticated user, not gated to
            // specific roles.
            Assert.Null(attribute!.Roles);
        }

        [Theory]
        [MemberData(nameof(ActionNames))]
        public void Action_HasNoOverrideThatWidensOrBypassesTheClassLevelAuthorize(string methodName)
        {
            var method = typeof(DlpDashboardController).GetMethods()
                .First(m => m.Name == methodName);

            Assert.Null(method.GetCustomAttribute<AuthorizeAttribute>());
            Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        }

        public static IEnumerable<object[]> ActionNames() => AllActionNames.Select(name => new object[] { name });
    }
}
