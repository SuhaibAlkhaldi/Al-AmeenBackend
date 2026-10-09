namespace DLPManagementSystem.DTO.FileTransfer
{
    // One browser observation of a send (Phase 7). The device and organization come from the device token, never the body.
    public class FileTransferEventBatchRequestDto
    {
        public Guid TenantId { get; set; }

        public Guid DeviceId { get; set; }

        public List<FileTransferObservationDto> Events { get; set; } = [];
    }

    public class FileTransferObservationDto
    {
        public Guid SendActionId { get; set; }

        public int RecipientOrdinal { get; set; }

        public string Channel { get; set; } = string.Empty;

        public string? RecipientRole { get; set; }

        public string RecipientKind { get; set; } = string.Empty;

        public string RecipientValue { get; set; } = string.Empty;

        // One of FileTransferEvidenceRules' RecipientEvidence_* values, decided by the extension from where it read the recipient.
        public string RecipientEvidenceType { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public string? FileHashBytes { get; set; }

        public string? ContentFingerprint { get; set; }

        public DateTimeOffset ObservedAtUtc { get; set; }

        public string? ExtensionVersion { get; set; }
    }

    // Counts only. A rejected observation is never stored, so the response reports why without listing the rejected data.
    public class FileTransferEventBatchResultDto
    {
        public int Accepted { get; set; }

        public int Duplicates { get; set; }

        public int RejectedInvalid { get; set; }

        public int RejectedNoFileMatch { get; set; }

        public int RejectedUnknownClassification { get; set; }
    }

    public class FileTransferEventListItemDto
    {
        public Guid Id { get; set; }

        public Guid SendActionId { get; set; }

        public int RecipientOrdinal { get; set; }

        public DateTimeOffset ObservedAtUtc { get; set; }

        public string? SourceEmployeeName { get; set; }

        public string SourceDeviceName { get; set; } = string.Empty;

        public string Channel { get; set; } = string.Empty;

        public string? RecipientRole { get; set; }

        public string RecipientKind { get; set; } = string.Empty;

        public string RecipientValue { get; set; } = string.Empty;

        public Guid? FileId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public string ClassificationTierAtEvent { get; set; } = string.Empty;

        public string FileEvidenceType { get; set; } = string.Empty;

        public string FileEvidenceLevel { get; set; } = string.Empty;

        public string RecipientEvidenceType { get; set; } = string.Empty;

        public string RecipientEvidenceLevel { get; set; } = string.Empty;

        public string OverallEvidenceLevel { get; set; } = string.Empty;

        public int CorrelationRuleVersion { get; set; }
    }
}
