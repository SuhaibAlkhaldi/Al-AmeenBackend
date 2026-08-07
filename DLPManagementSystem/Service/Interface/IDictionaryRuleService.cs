using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.DictionaryRules;

namespace DLPManagementSystem.Service.Interface
{
    public interface IDictionaryRuleService
    {
        // Used by both the admin "current rules" endpoint and the agent pull endpoint, so they can
        // never disagree about what "active" means.
        Task<DictionaryRulesResponseDto> GetActiveAsync(Guid organizationId, CancellationToken cancellationToken = default);

        Task<ApiResponse<DictionaryRulesResponseDto>> SaveRulesAsync(
            Guid organizationId,
            Guid savedByUserId,
            List<DictionaryRuleItemDto> rules,
            CancellationToken cancellationToken = default);
    }
}
