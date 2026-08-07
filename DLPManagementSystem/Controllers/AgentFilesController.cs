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
        private readonly IDictionaryRuleService _dictionaryRuleService;

        public AgentFilesController(
            IFileClassificationService fileClassificationService,
            IFileKeyProtectionService fileKeyProtectionService,
            IDictionaryRuleService dictionaryRuleService)
        {
            _fileClassificationService = fileClassificationService;
            _fileKeyProtectionService = fileKeyProtectionService;
            _dictionaryRuleService = dictionaryRuleService;
        }

        // A separate pull from the policy blob - dictionary rules change independently of the rest
        // of policy, so the agent's DictionaryRuleSyncWorker polls this on its own cadence. Returns
        // the raw DTO (unwrapped, not ApiResponse<T>) since the agent's BackendApiClient always
        // succeeds in reading a rule set (falls back to defaults) - there's no failure case to wrap.
        [HttpGet("api/v1/agent/dictionary-rules")]
        public async Task<IActionResult> GetDictionaryRules(CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _dictionaryRuleService.GetActiveAsync(organizationId, cancellationToken);
            return Ok(response);
        }

        // Multipart, not JSON: the file's actual bytes travel here for background-scan classification
        // (CompanyDlp.Service's FileInventoryScanner is the only agent-side caller that attaches one -
        // see IFileClassificationService.ClassifyAsync's fileContent parameter for why). The "file"
        // part is optional; every other caller omits it and gets the extension-blocklist fallback.
        [HttpPost("api/v1/agent/file-classification")]
        [RequestSizeLimit(2L * 1024 * 1024 * 1024)]
        public async Task<IActionResult> ClassifyFile([FromForm] string request, IFormFile? file, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var deviceId = User.GetDeviceId();

            FileClassificationRequestDto? parsedRequest;
            try
            {
                parsedRequest = System.Text.Json.JsonSerializer.Deserialize<FileClassificationRequestDto>(
                    request,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest("request field is not valid JSON.");
            }

            if (parsedRequest == null)
            {
                return BadRequest("request field is required.");
            }

            await using var fileStream = file?.OpenReadStream();
            var response = await _fileClassificationService.ClassifyAsync(organizationId, deviceId, parsedRequest, fileStream, cancellationToken);

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
