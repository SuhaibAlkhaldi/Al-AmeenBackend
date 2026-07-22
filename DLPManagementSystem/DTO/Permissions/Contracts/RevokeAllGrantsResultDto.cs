namespace DLPManagementSystem.DTO.Permissions.Contracts
{
    public sealed class RevokeAllGrantsResultDto
    {
        public int RevokedCount { get; set; }

        public List<string> ActionKeys { get; set; } = new();
    }
}
