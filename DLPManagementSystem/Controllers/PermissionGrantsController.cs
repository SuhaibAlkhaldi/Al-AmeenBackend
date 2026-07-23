using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/permission-grants")]
    [Authorize]
    public class PermissionGrantsController : ControllerBase
    {
        private readonly IPermissionGrantService _permissionGrantService;

        public PermissionGrantsController(IPermissionGrantService permissionGrantService)
        {
            _permissionGrantService = permissionGrantService;
        }

        [HttpGet]
        public async Task<IActionResult> GetGrants(
            [FromQuery] string? subjectId,
            [FromQuery] string? actionKey,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var userTypeId = User.GetUserTypeId();
            var response = await _permissionGrantService.GetGrantsAsync(organizationId, userId, userTypeId, subjectId, actionKey, status, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpPost("{id:guid}/revoke")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
        public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokePermissionGrantDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var response = await _permissionGrantService.RevokeAsync(organizationId, id, userId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("revoke-all")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
        public async Task<IActionResult> RevokeAll([FromBody] RevokeAllGrantsDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var response = await _permissionGrantService.RevokeAllAsync(organizationId, userId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
