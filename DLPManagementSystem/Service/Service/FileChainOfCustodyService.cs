using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileChainOfCustody;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    // Phase 2 of the "File Tracking" plan: given a file, find every device/employee that has been
    // observed holding the exact same content (same SHA-256), across the whole organization, ordered
    // by when each of them first had it. Deliberately hash-based, not FileId-based - a rename creates a
    // brand-new FileId (see FileInventoryRecord's class comment), and content that's been copied to an
    // unrelated path on another device has its own unrelated FileId too; the hash is the only thing
    // that actually identifies "the same content" across all of that.
    //
    // No new tables - reads from two existing sources and merges them:
    //   1. FileVersionHistory (Phase 1) - every genuine transition into this hash, across all devices.
    //      This is the complete, precise source going forward.
    //   2. FileInventoryRecords' current state - a fallback for files whose active row predates Phase
    //      1's deployment (so it has no history rows yet); FirstSeenAtUtc is the best available
    //      approximation of when that device first had this content.
    // Where both sources have an entry for the same (device, employee), the earlier timestamp wins.
    public class FileChainOfCustodyService : IFileChainOfCustodyService
    {
        private readonly DLPSystemContext _db;

        public FileChainOfCustodyService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<FileChainOfCustodyResultDto>> GetChainOfCustodyAsync(
            Guid organizationId,
            Guid fileId,
            CancellationToken cancellationToken = default)
        {
            // Not filtered on IsDeleted - a renamed or deleted file's chain of custody is still a
            // meaningful thing to look up (e.g. investigating a file that was since removed).
            var record = await _db.FileInventoryRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FileId == fileId && x.OrganizationId == organizationId, cancellationToken);

            if (record == null)
            {
                return ApiResponse<FileChainOfCustodyResultDto>.FailureResponse(
                    "File was not found.",
                    "الملف غير موجود");
            }

            var fingerprint = record.ContentFingerprint;

            if (string.IsNullOrEmpty(record.FileHash) && string.IsNullOrEmpty(fingerprint))
            {
                // Nothing to correlate on - return an empty chain rather than an error.
                return ApiResponse<FileChainOfCustodyResultDto>.SuccessResponse(new FileChainOfCustodyResultDto
                {
                    FileId = fileId,
                    FileHash = string.Empty,
                    Chain = new List<FileChainOfCustodyEntryDto>()
                });
            }

            var hash = record.FileHash ?? string.Empty;

            // A file with a content fingerprint is matched by that fingerprint, so copies that differ only
            // by the classification header each device wrote into them still correlate. History rows carry
            // only file hashes, so they keep matching by hash.
            var fromHistory = await _db.FileVersionHistories
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.NewFileHash == hash)
                .GroupBy(x => new { x.DeviceId, x.EmployeeId })
                .Select(g => new { g.Key.DeviceId, g.Key.EmployeeId, FirstObservedAtUtc = g.Min(x => x.ObservedAtUtc) })
                .ToListAsync(cancellationToken);

            var fromCurrentRecords = await _db.FileInventoryRecords
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && !x.IsDeleted
                    && (fingerprint != null ? x.ContentFingerprint == fingerprint : x.FileHash == hash))
                .GroupBy(x => new { x.DeviceId, x.EmployeeId })
                .Select(g => new { g.Key.DeviceId, g.Key.EmployeeId, FirstObservedAtUtc = g.Min(x => x.FirstSeenAtUtc) })
                .ToListAsync(cancellationToken);

            var merged = new Dictionary<(Guid DeviceId, Guid? EmployeeId), DateTimeOffset>();

            foreach (var entry in fromHistory)
            {
                merged[(entry.DeviceId, entry.EmployeeId)] = entry.FirstObservedAtUtc;
            }

            foreach (var entry in fromCurrentRecords)
            {
                var key = (entry.DeviceId, entry.EmployeeId);
                if (!merged.TryGetValue(key, out var existing) || entry.FirstObservedAtUtc < existing)
                {
                    merged[key] = entry.FirstObservedAtUtc;
                }
            }

            var deviceIds = merged.Keys.Select(k => k.DeviceId).Distinct().ToList();
            var employeeIds = merged.Keys
                .Select(k => k.EmployeeId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var deviceNames = await _db.Devices
                .AsNoTracking()
                .Where(x => deviceIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.MachineName, cancellationToken);

            var employeeNames = await _db.Employees
                .AsNoTracking()
                .Where(x => employeeIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

            var chain = merged
                .Select(kv => new FileChainOfCustodyEntryDto
                {
                    DeviceId = kv.Key.DeviceId,
                    DeviceName = deviceNames.TryGetValue(kv.Key.DeviceId, out var deviceName) ? deviceName : "(unknown device)",
                    EmployeeId = kv.Key.EmployeeId,
                    EmployeeName = kv.Key.EmployeeId.HasValue && employeeNames.TryGetValue(kv.Key.EmployeeId.Value, out var employeeName)
                        ? employeeName
                        : null,
                    FirstObservedAtUtc = kv.Value
                })
                .OrderBy(x => x.FirstObservedAtUtc)
                .ToList();

            var copyRecords = await _db.FileInventoryRecords
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && !x.IsDeleted && x.FileId != fileId
                    && (fingerprint != null ? x.ContentFingerprint == fingerprint : x.FileHash == hash))
                .Select(x => new { x.FileId, x.FileName, x.DeviceId, x.FirstSeenAtUtc })
                .ToListAsync(cancellationToken);

            var copyDeviceIds = copyRecords.Select(x => x.DeviceId).Distinct().ToList();
            var copyDeviceNames = await _db.Devices
                .AsNoTracking()
                .Where(x => copyDeviceIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.MachineName, cancellationToken);

            var otherFileCopies = copyRecords
                .Select(x => new FileCopyEntryDto
                {
                    FileId = x.FileId,
                    FileName = x.FileName,
                    DeviceId = x.DeviceId,
                    DeviceName = copyDeviceNames.TryGetValue(x.DeviceId, out var copyDeviceName) ? copyDeviceName : "(unknown device)",
                    IsSameDevice = x.DeviceId == record.DeviceId,
                    FirstSeenAtUtc = x.FirstSeenAtUtc
                })
                .OrderBy(x => x.FirstSeenAtUtc)
                .ToList();

            return ApiResponse<FileChainOfCustodyResultDto>.SuccessResponse(new FileChainOfCustodyResultDto
            {
                FileId = fileId,
                FileHash = hash,
                Chain = chain,
                OtherFileCopies = otherFileCopies
            });
        }
    }
}
