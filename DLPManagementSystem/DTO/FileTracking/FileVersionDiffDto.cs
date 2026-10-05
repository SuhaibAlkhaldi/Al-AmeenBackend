namespace DLPManagementSystem.DTO.FileTracking
{
    // What changed in one file version compared with the version before it (File Tracking Phase 6).
    // When IsAvailable is false, Lines is empty and UnavailableReason says why:
    //   text_not_available           - this version's text was not captured (public tier, too large, or past retention)
    //   previous_text_not_available  - the version before it has no captured text, so there is nothing to compare
    //   too_many_changes             - more changed lines than the comparison limit (see LineDiff.MaxEditDistance)
    //   not_a_content_change         - the row is a classification or copy event, not a content change
    public class FileVersionDiffDto
    {
        public Guid VersionId { get; set; }

        // Null when this is the first recorded version of the file, which is compared against empty text.
        public Guid? PreviousVersionId { get; set; }

        public bool IsFirstVersion { get; set; }

        public Guid FileId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string? DeviceName { get; set; }

        public string? EmployeeName { get; set; }

        public DateTimeOffset ObservedAtUtc { get; set; }

        public string ClassificationTier { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }

        public string? UnavailableReason { get; set; }

        public int AddedLineCount { get; set; }

        public int RemovedLineCount { get; set; }

        // Changed lines with a few unchanged lines of context. Unchanged runs further away are replaced by one
        // "gap" entry, so the reviewer never receives more of the document than the change needs.
        public List<FileVersionDiffLineDto> Lines { get; set; } = [];
    }

    public class FileVersionDiffLineDto
    {
        // "equal", "added", "removed", or "gap" (a folded run of unchanged lines; see SkippedLineCount).
        public string Kind { get; set; } = string.Empty;

        public string? Text { get; set; }

        public int? OldLineNumber { get; set; }

        public int? NewLineNumber { get; set; }

        public int? SkippedLineCount { get; set; }
    }
}
