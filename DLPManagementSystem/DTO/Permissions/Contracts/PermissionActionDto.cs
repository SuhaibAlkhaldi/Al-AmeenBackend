namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class PermissionActionDto
    {
        public string Key { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string DefaultDecision { get; set; } = string.Empty;

        public bool SupportsPermanentGrant { get; set; }

        public bool SupportsTemporaryGrant { get; set; }

        public int SortOrder { get; set; }
    }
}
