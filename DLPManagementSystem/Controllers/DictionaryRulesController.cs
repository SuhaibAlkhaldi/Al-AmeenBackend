using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.DictionaryRules;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    // Admin-facing CRUD for the classification dictionary rules configured on the dashboard's
    // "Rules" page. Deliberately "overwrite the whole active list" rather than per-rule endpoints -
    // the evaluator always considers the whole active rule set together (highest-severity-wins
    // across all matching rules), so editing rules one at a time in isolation isn't really a
    // separate operation from "save the new full list" either.
    [ApiController]
    [Route("api/v1/dictionary-rules")]
    [Authorize]
    public class DictionaryRulesController : ControllerBase
    {
        private readonly IDictionaryRuleService _dictionaryRuleService;

        public DictionaryRulesController(IDictionaryRuleService dictionaryRuleService)
        {
            _dictionaryRuleService = dictionaryRuleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRules(CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var response = await _dictionaryRuleService.GetActiveAsync(organizationId, cancellationToken);
            return Ok(ApiResponse<DictionaryRulesResponseDto>.SuccessResponse(response));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,SecurityAdmin")]
        public async Task<IActionResult> SaveRules([FromBody] List<DictionaryRuleItemDto> rules, CancellationToken cancellationToken)
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();
            var response = await _dictionaryRuleService.SaveRulesAsync(organizationId, userId, rules, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
