namespace DLPManagementSystem.DTO.AgentFileInventory
{
    // Mirrors CompanyDlp.Service.FileInventorySyncWorker's SyncKind values on the agent side. The only
    // behavior this drives server-side: a Reconciliation batch may legitimately contain a Deleted for a
    // path that already has no active row (an earlier Incremental batch already deleted it, or it was
    // never synced in the first place) - that must be treated as already-satisfied, not rejected. An
    // InitialFull batch never contains Renamed (there is nothing to rename from on a first sync).
    public static class FileInventorySyncKinds
    {
        public const string InitialFull = "InitialFull";
        public const string Incremental = "Incremental";
        public const string Reconciliation = "Reconciliation";
    }

    public static class FileInventoryChangeTypes
    {
        public const string Created = "Created";
        public const string Modified = "Modified";
        public const string Renamed = "Renamed";
        public const string Deleted = "Deleted";
    }

    // Mirrors CompanyDlp.Service.FileProvenanceChannels/FileProvenanceOrigins on the agent side -
    // SelfCreated stands in for FileProvenanceOrigins.SelfCreated (no receiving channel matched); the
    // other three are FileProvenanceChannels' positive-match channels.
    public static class FileInventoryProvenance
    {
        public const string BrowserDownload = "BrowserDownload";
        public const string Usb = "Usb";
        public const string DesktopApp = "DesktopApp";
        public const string SelfCreated = "SelfCreated";
    }

    public class AgentFileInventoryBatchRequestDto
    {
        public Guid TenantId { get; set; }

        public Guid DeviceId { get; set; }

        public string AgentVersion { get; set; } = string.Empty;

        public string SyncKind { get; set; } = FileInventorySyncKinds.Incremental;

        public List<FileInventoryChangeDto> Changes { get; set; } = new();
    }

    public class FileInventoryChangeDto
    {
        // Client-generated. Not used as a persisted idempotency key (see AgentFileInventoryService's
        // class comment for why an upsert-based model doesn't need one) - carried through only so the
        // response can tell the agent which of its outbox items to clear.
        public Guid ChangeId { get; set; }

        public string ChangeType { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        // Renamed only.
        public string? OldFilePath { get; set; }

        public string? FileHash { get; set; }

        // Optional - when set, overrides the extension the server would otherwise derive from
        // FilePath's literal suffix. Needed for an encrypted .dlpenc file: FilePath's own suffix is
        // never the file's real type, and reporting "dlpenc" as the File Type column would be
        // meaningless - the agent resolves the real underlying extension (see
        // FileInventoryContentResolver.ResolveEncryptedAsync) and sends it here instead.
        public string? Extension { get; set; }

        public long? SizeBytes { get; set; }

        public string? ClassificationTier { get; set; }

        public string? Provenance { get; set; }

        public bool? IsProtected { get; set; }

        // True when the agent itself just rewrote this content (its own classification watermark), not
        // a user edit - see the agent's SelfWrittenContentRegistry. Such a change never records a history
        // row by itself; only a real classification or protection change does.
        public bool? IsSystemRewrite { get; set; }

        // See FileInventoryRecord.ContentFingerprint. Null when the agent cannot read this file's format.
        public string? ContentFingerprint { get; set; }

        // The file's extracted text, sent only for the sensitive tiers (Restricted, Secret, Very Secret) and
        // only when it fits the agent's capture limit. Stored encrypted for the diff viewer (see
        // FileVersionContent) and never written to logs. Null means the version has no text to compare.
        public string? ExtractedText { get; set; }

        public DateTimeOffset OccurredAtUtc { get; set; }
    }
}
