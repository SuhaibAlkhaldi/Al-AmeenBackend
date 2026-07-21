using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Employees;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/employees")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk,Auditor")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees(
            [FromQuery] string? search,
            [FromQuery] Guid? departmentId,
            [FromQuery] int? statusId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _employeeService.GetEmployeesAsync(organizationId, search, departmentId, statusId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEmployeeById(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _employeeService.GetEmployeeByIdAsync(organizationId, id, cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HelpDesk")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _employeeService.CreateEmployeeAsync(organizationId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,HelpDesk")]
        public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _employeeService.UpdateEmployeeAsync(organizationId, id, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,HelpDesk")]
        public async Task<IActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _employeeService.DeleteEmployeeAsync(organizationId, id, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
