using System.Text.Json;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;
using DLPManagementSystem.DTO.DictionaryRules;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class DictionaryRuleService : IDictionaryRuleService
    {
        // Same fallback the C# and Python evaluators both use when no admin-configured rules exist
        // yet, shown here too so the Rules page has something sensible to display/edit from a fresh
        // organization.
        private static readonly List<DictionaryRuleItemDto> DefaultRules = new()
        {
            new DictionaryRuleItemDto { Entities = new() { "PHONE", "PASSPORT" }, Condition = "AND", Severity = ClassificationTiers.VerySecret },
            new DictionaryRuleItemDto { Entities = new() { "PASSPORT" }, Condition = "OR", Severity = ClassificationTiers.Secret },
            new DictionaryRuleItemDto { Entities = new() { "PHONE" }, Condition = "OR", Severity = ClassificationTiers.Internal }
        };

        private readonly DLPSystemContext _db;

        public DictionaryRuleService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<DictionaryRulesResponseDto> GetActiveAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            var active = await _db.DictionaryRules.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.IsActive)
                .SingleOrDefaultAsync(cancellationToken);

            if (active is null)
            {
                return new DictionaryRulesResponseDto { Version = 0, Rules = DefaultRules };
            }

            var rules = JsonSerializer.Deserialize<List<DictionaryRuleItemDto>>(active.RulesJson) ?? new();
            return new DictionaryRulesResponseDto { Version = active.Version, Rules = rules };
        }

        public async Task<ApiResponse<DictionaryRulesResponseDto>> SaveRulesAsync(
            Guid organizationId,
            Guid savedByUserId,
            List<DictionaryRuleItemDto> rules,
            CancellationToken cancellationToken = default)
        {
            if (rules.Count == 0)
            {
                return ApiResponse<DictionaryRulesResponseDto>.FailureResponse(
                    "At least one rule is required.", "لازم قاعدة وحدة على الأقل.");
            }

            foreach (var rule in rules)
            {
                if (!rule.Condition.Equals("AND", StringComparison.OrdinalIgnoreCase)
                    && !rule.Condition.Equals("OR", StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResponse<DictionaryRulesResponseDto>.FailureResponse(
                        "Condition must be AND or OR.", "الشرط لازم يكون AND أو OR.");
                }

                if (!ClassificationTiers.Order.Contains(rule.Severity))
                {
                    return ApiResponse<DictionaryRulesResponseDto>.FailureResponse(
                        "Unsupported severity.", "تصنيف غير مدعوم.");
                }

                if (rule.Entities.Count == 0 && rule.TextKeywords.Count == 0)
                {
                    return ApiResponse<DictionaryRulesResponseDto>.FailureResponse(
                        "Each rule needs at least one entity type or keyword.", "كل قاعدة لازم فيها entity أو كلمة مفتاحية وحدة على الأقل.");
                }
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var currentVersion = await _db.DictionaryRules.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.IsActive)
                .Select(x => (long?)x.Version)
                .SingleOrDefaultAsync(cancellationToken) ?? 0;

            await _db.DictionaryRules
                .Where(x => x.OrganizationId == organizationId && x.IsActive)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false), cancellationToken);

            var entity = new DictionaryRule
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Version = currentVersion + 1,
                RulesJson = JsonSerializer.Serialize(rules),
                IsActive = true,
                CreatedByUserId = savedByUserId
            };
            _db.DictionaryRules.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ApiResponse<DictionaryRulesResponseDto>.SuccessResponse(
                new DictionaryRulesResponseDto { Version = entity.Version, Rules = rules },
                "Rules saved.", "تم حفظ القواعد.");
        }
    }
}
