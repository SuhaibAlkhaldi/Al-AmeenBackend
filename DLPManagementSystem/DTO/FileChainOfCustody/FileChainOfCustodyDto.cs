namespace DLPManagementSystem.DTO.FileChainOfCustody
{
    // Phase 2 of the "File Tracking" plan (see the approved plan file). One entry per device/employee
    // that has been observed holding this exact content (same SHA-256), ordered chronologically by
    // first observation. Deliberately named/shaped to stay neutral - matching content across devices is
    // NOT proof of an actual transfer (could be email, USB, a shared folder, or independent possession
    // of the same content). Consumers (the File History timeline page, alert descriptions) must render
    // this as "Full Copy Detected / Source.../Target.../Detection: Same SHA-256" - never as
    // "Sent from A to B".
    public class FileChainOfCustodyEntryDto
    {
        public Guid DeviceId { get; set; }

        public string DeviceName { get; set; } = string.Empty;

        public Guid? EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public DateTimeOffset FirstObservedAtUtc { get; set; }
    }

    public class FileChainOfCustodyResultDto
    {
        public Guid FileId { get; set; }

        public string FileHash { get; set; } = string.Empty;

        // Chronologically ascending - Chain[0] is whoever was first observed with this exact content.
        public List<FileChainOfCustodyEntryDto> Chain { get; set; } = new();

        // Other files (not this one) currently holding the same content, including ones on this same
        // device. Chain above is grouped per device, so it cannot tell a second file on this device apart
        // from this file itself - this list is per file and always excludes the file being looked up.
        public List<FileCopyEntryDto> OtherFileCopies { get; set; } = new();
    }

    public class FileCopyEntryDto
    {
        public Guid FileId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public Guid DeviceId { get; set; }

        public string DeviceName { get; set; } = string.Empty;

        public bool IsSameDevice { get; set; }

        public DateTimeOffset FirstSeenAtUtc { get; set; }
    }
}
