namespace DLPManagementSystem.DTO.EnrollmentTokens
{
    public class EnrollmentTokenDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public int MaxUses { get; set; }

        public int UsedCount { get; set; }

        public DateTimeOffset? RevokedAtUtc { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public Guid CreatedByUserId { get; set; }

        public string CreatedByUserName { get; set; } = string.Empty;
    }
}
