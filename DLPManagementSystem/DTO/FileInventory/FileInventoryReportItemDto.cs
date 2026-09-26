namespace DLPManagementSystem.DTO.FileInventory
{
    // The File Inventory Report's 10 columns, in the agreed export column order.
    public class FileInventoryReportItemDto
    {
        public Guid FileId { get; set; }
        public string? EmployeeName { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ClassificationTier { get; set; } = string.Empty;
        public string? FileTypeName { get; set; }
        public long SizeBytes { get; set; }
        public string Provenance { get; set; } = string.Empty;
        public bool IsProtected { get; set; }
        public DateTimeOffset LastScanAtUtc { get; set; }
    }
}
