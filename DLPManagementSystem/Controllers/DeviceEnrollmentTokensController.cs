using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.EnrollmentTokens;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/device-enrollment-tokens")]
    [Authorize(Roles = "SuperAdmin")]
    public class DeviceEnrollmentTokensController : ControllerBase
    {
        private readonly IEnrollmentTokenService _enrollmentTokenService;

        public DeviceEnrollmentTokensController(IEnrollmentTokenService enrollmentTokenService)
        {
            _enrollmentTokenService = enrollmentTokenService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTokens(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _enrollmentTokenService.GetTokensAsync(organizationId, page, pageSize, cancellationToken);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateToken([FromBody] CreateEnrollmentTokenDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var createdByUserId = User.GetUserId();

            var response = await _enrollmentTokenService.CreateTokenAsync(organizationId, createdByUserId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("{id:guid}/revoke")]
        public async Task<IActionResult> RevokeToken(Guid id, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _enrollmentTokenService.RevokeTokenAsync(organizationId, id, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
