namespace DLPManagementSystem.DTO.FileChainOfCustody
{
    // Phase 1's own raw history, exposed for Phase 3's timeline page - one entry per genuine change
    // recorded for this specific FileId (see FileVersionHistory's class comment), oldest first. The
    // very first entry is always the "Genesis" row (Old* all null).
    public class FileVersionHistoryEntryDto
    {
        public DateTimeOffset ObservedAtUtc { get; set; }

        public string? EmployeeName { get; set; }

        public string? OldFileHash { get; set; }

        public string NewFileHash { get; set; } = string.Empty;

        public string? OldClassificationTier { get; set; }

        public string NewClassificationTier { get; set; } = string.Empty;

        public bool? OldIsProtected { get; set; }

        public bool NewIsProtected { get; set; }

        public bool ContentChanged { get; set; }

        public bool ClassificationChanged { get; set; }

        public bool ProtectionChanged { get; set; }

        // "Change" or "FullCopy" (see FileVersionHistory.EventType). For FullCopy only: the file this
        // content was found identical to, and the device that file was on.
        public string EventType { get; set; } = "Change";

        public Guid? SourceFileId { get; set; }

        public string? SourceFileName { get; set; }

        public Guid? SourceDeviceId { get; set; }

        public string? SourceDeviceName { get; set; }
    }

    public class FileVersionHistoryResultDto
    {
        public Guid FileId { get; set; }

        public string FileName { get; set; } = string.Empty;

        // This file's own device/employee - lets a caller reliably exclude "this same record"
        // from FileChainOfCustodyResultDto's cross-device results (matching by DeviceId, not by
        // guessing from a Genesis entry that may not exist yet - see FileChainOfCustodyService's
        // FileInventoryRecords fallback for files that predate Phase 1).
        public Guid DeviceId { get; set; }

        public Guid? EmployeeId { get; set; }

        // Chronologically ascending, oldest (Genesis) first. Can be empty for a file whose active
        // row predates Phase 1's deployment - it simply has no history rows yet.
        public List<FileVersionHistoryEntryDto> Entries { get; set; } = new();
    }
}
