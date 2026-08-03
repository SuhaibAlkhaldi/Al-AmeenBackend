using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Devices;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/devices")]
    [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk,Auditor")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DevicesController(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDevices(
            [FromQuery] string? search,
            [FromQuery] int? statusId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageSize = PagingDefaults.ClampPageSize(pageSize);
            var organizationId = User.GetOrganizationId();
            var response = await _deviceService.GetDevicesAsync(organizationId, search, statusId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDeviceById(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _deviceService.GetDeviceByIdAsync(organizationId, id, cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk")]
        public async Task<IActionResult> UpdateDevice(Guid id, [FromBody] UpdateDeviceDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var callerUserId = User.GetUserId();
            var response = await _deviceService.UpdateDeviceAsync(organizationId, id, callerUserId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk")]
        public async Task<IActionResult> DeleteDevice(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var callerUserId = User.GetUserId();
            var response = await _deviceService.DeleteDeviceAsync(organizationId, id, callerUserId, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/assign")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk")]
        public async Task<IActionResult> AssignDevice(Guid id, [FromBody] AssignDeviceDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var assignedByUserId = User.GetUserId();
            var response = await _deviceService.AssignDeviceAsync(organizationId, id, assignedByUserId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/unassign")]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin,HelpDesk")]
        public async Task<IActionResult> UnassignDevice(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _deviceService.UnassignDeviceAsync(organizationId, id, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
