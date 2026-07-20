using DLPManagementSystem.Authentication;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = DeviceBearerDefaults.SchemeName)]
    public class AgentFilesController : ControllerBase
    {
        private readonly IFileClassificationService _fileClassificationService;
        private readonly IFileKeyProtectionService _fileKeyProtectionService;

        public AgentFilesController(
            IFileClassificationService fileClassificationService,
            IFileKeyProtectionService fileKeyProtectionService)
        {
            _fileClassificationService = fileClassificationService;
            _fileKeyProtectionService = fileKeyProtectionService;
        }

        [HttpPost("api/v1/agent/file-classification")]
        public async Task<IActionResult> ClassifyFile([FromBody] FileClassificationRequestDto request, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            var response = await _fileClassificationService.ClassifyAsync(organizationId, deviceId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response.Data);
        }

        [HttpPost("api/v1/agent/file-keys/wrap")]
        public IActionResult WrapFileKey([FromBody] FileKeyWrapRequestDto request)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            if (request.TenantId != organizationId || request.DeviceId != deviceId)
            {
                return BadRequest("tenantId/deviceId do not match the authenticated device.");
            }

            var response = _fileKeyProtectionService.Wrap(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response.Data);
        }

        [HttpPost("api/v1/agent/file-keys/unwrap")]
        public IActionResult UnwrapFileKey([FromBody] FileKeyUnwrapRequestDto request)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            if (request.TenantId != organizationId || request.DeviceId != deviceId)
            {
                return BadRequest("tenantId/deviceId do not match the authenticated device.");
            }

            var response = _fileKeyProtectionService.Unwrap(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response.Data);
        }
    }
}
