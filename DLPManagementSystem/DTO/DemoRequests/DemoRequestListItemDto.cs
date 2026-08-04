namespace DLPManagementSystem.DTO.DemoRequests
{
    public class DemoRequestListItemDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string CompanyEmail { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string CompanySize { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
