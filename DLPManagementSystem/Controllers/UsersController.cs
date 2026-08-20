using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Users;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk,Auditor")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] int? roleId,
            [FromQuery] int? statusId,
            [FromQuery] int? userTypeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var organizationId = User.GetOrganizationId();
            var response = await _userService.GetUsersAsync(organizationId, search, roleId, statusId, userTypeId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _userService.GetUserByIdAsync(organizationId, id, cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HelpDesk")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var callerUserId = User.GetUserId();
            var callerRoleName = User.GetRoleName();
            var response = await _userService.CreateUserAsync(organizationId, callerUserId, callerRoleName, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,HelpDesk")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var callerUserId = User.GetUserId();
            var callerRoleName = User.GetRoleName();
            var response = await _userService.UpdateUserAsync(organizationId, id, callerUserId, callerRoleName, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/reset-password")]
        [Authorize(Roles = "SuperAdmin,HelpDesk")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var callerUserId = User.GetUserId();
            var response = await _userService.ResetPasswordAsync(organizationId, id, callerUserId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,HelpDesk")]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var callerUserId = User.GetUserId();
            var response = await _userService.DeleteUserAsync(organizationId, id, callerUserId, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
