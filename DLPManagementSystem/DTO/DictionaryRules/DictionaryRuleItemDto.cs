using System.ComponentModel.DataAnnotations;

namespace DLPManagementSystem.DTO.DictionaryRules
{
    // Mirrors the agent's CompanyDlp.Contracts.DictionaryRuleItem shape exactly (entities + condition
    // + severity, plus an optional text_keywords list), and the AI DLP system's original rule shape
    // ({"entities": [...], "condition": "AND"|"OR", "severity": "..."}). This is the JSON that gets
    // serialized into DictionaryRule.RulesJson.
    public sealed class DictionaryRuleItemDto
    {
        [Required]
        public List<string> Entities { get; set; } = new();

        [Required]
        [StringLength(10)]
        public string Condition { get; set; } = "OR";

        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = "Public";

        public List<string> TextKeywords { get; set; } = new();
    }

    public sealed class DictionaryRulesResponseDto
    {
        public long Version { get; set; }

        public List<DictionaryRuleItemDto> Rules { get; set; } = new();
    }
}
