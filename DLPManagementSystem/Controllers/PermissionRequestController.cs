using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/permission-requests")]
    [Authorize]
    public class PermissionRequestController : ControllerBase
    {
        private readonly IPermissionRequestService _permissionRequestService;

        public PermissionRequestController(IPermissionRequestService permissionRequestService)
        {
            _permissionRequestService = permissionRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests(
            [FromQuery] int? statusId,
            [FromQuery] Guid? requestedByEmployeeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var userTypeId = User.GetUserTypeId();
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var response = await _permissionRequestService.GetRequestsAsync(organizationId, userId, userTypeId, statusId, requestedByEmployeeId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("pending-count")]
        public async Task<IActionResult> GetPendingCount(CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var userTypeId = User.GetUserTypeId();
            var response = await _permissionRequestService.GetPendingCountAsync(organizationId, userId, userTypeId, cancellationToken);
            return Ok(response);
        }

        [HttpGet("source-event/{correlationId:guid}")]
        public async Task<IActionResult> GetSourceEventDetails(Guid correlationId, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var userTypeId = User.GetUserTypeId();
            var response = await _permissionRequestService.GetSourceEventDetailsAsync(organizationId, userId, userTypeId, correlationId, cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var userTypeId = User.GetUserTypeId();
            var response = await _permissionRequestService.GetByIdAsync(organizationId, id, userId, userTypeId, cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePermissionRequestDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var userTypeId = User.GetUserTypeId();
            var response = await _permissionRequestService.CreateAsync(organizationId, userId, userTypeId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewPermissionRequestDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var userTypeId = User.GetUserTypeId();
            var response = await _permissionRequestService.ApproveAsync(organizationId, id, userId, userTypeId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewPermissionRequestDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var userTypeId = User.GetUserTypeId();
            var response = await _permissionRequestService.RejectAsync(organizationId, id, userId, userTypeId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
