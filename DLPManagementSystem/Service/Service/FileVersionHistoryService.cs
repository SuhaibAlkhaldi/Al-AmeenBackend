using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileChainOfCustody;
using DLPManagementSystem.Helper.FileTracking;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    // Phase 1's own raw history, exposed for Phase 3's "File History" timeline page - this is the
    // "what happened to this specific file over time" half; FileChainOfCustodyService (Phase 2)
    // supplies the "where else has this content been seen" half. The page combines both.
    public class FileVersionHistoryService : IFileVersionHistoryService
    {
        private readonly DLPSystemContext _db;

        public FileVersionHistoryService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<FileVersionHistoryResultDto>> GetHistoryAsync(
            Guid organizationId,
            Guid fileId,
            CancellationToken cancellationToken = default)
        {
            var record = await _db.FileInventoryRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FileId == fileId && x.OrganizationId == organizationId, cancellationToken);

            if (record == null)
            {
                return ApiResponse<FileVersionHistoryResultDto>.FailureResponse(
                    "File was not found.",
                    "الملف غير موجود");
            }

            // One file's lineage spans every FileId that ever sat at the same device, folder and base name
            // (see FileLineage), so the original creation stays visible after the file was re-tagged.
            var lineage = await FileLineage.ResolveAsync(_db, organizationId, record, cancellationToken);

            var lineageFileIds = lineage.Select(x => x.FileId).ToList();
            var earliestSeenAtUtc = lineage.Min(x => x.FirstSeenAtUtc);

            var rows = await _db.FileVersionHistories
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x => x.OrganizationId == organizationId && lineageFileIds.Contains(x.FileId))
                .OrderBy(x => x.ObservedAtUtc)
                .ToListAsync(cancellationToken);

            // Only whether a version has captured text is exposed here, never the text itself. The diff endpoint
            // (FileVersionDiffController) is the one place the text is returned, and only to SuperAdmin/SecurityAdmin.
            var versionsWithText = (await _db.FileVersionContents
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && lineageFileIds.Contains(x.FileId))
                .Select(x => x.FileVersionHistoryId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var entries = new List<FileVersionHistoryEntryDto>();
            FileVersionHistoryEntryDto? previous = null;
            foreach (var row in rows)
            {
                var entry = new FileVersionHistoryEntryDto
                {
                    VersionId = row.Id,
                    CanCompare = row.EventType == "Change" && row.ContentChanged && versionsWithText.Contains(row.Id),
                    ObservedAtUtc = row.ObservedAtUtc,
                    EmployeeName = row.Employee?.DisplayName,
                    OldFileHash = row.OldFileHash,
                    NewFileHash = row.NewFileHash,
                    OldClassificationTier = row.OldClassificationTier,
                    NewClassificationTier = row.NewClassificationTier,
                    OldIsProtected = row.OldIsProtected,
                    NewIsProtected = row.NewIsProtected,
                    ContentChanged = row.ContentChanged,
                    ClassificationChanged = row.ClassificationChanged,
                    ProtectionChanged = row.ProtectionChanged,
                    EventType = row.EventType,
                    SourceFileId = row.SourceFileId
                };

                // A FullCopy event is not a state transition: it is kept as recorded and does not take part
                // in the continuation logic below, nor become the baseline for the next row.
                if (row.EventType == "FullCopy")
                {
                    entries.Add(entry);
                    continue;
                }

                // A successor FileId's own "Genesis" row (Old* all null) is really a continuation of the
                // previous FileId's state at this same lineage - express it against that state, and drop
                // it entirely when nothing actually changed between them.
                var isSuccessorGenesis = previous != null
                    && row.OldFileHash == null && row.OldClassificationTier == null && row.OldIsProtected == null;
                if (isSuccessorGenesis)
                {
                    entry.OldFileHash = previous!.NewFileHash;
                    entry.OldClassificationTier = previous.NewClassificationTier;
                    entry.OldIsProtected = previous.NewIsProtected;
                    entry.ContentChanged = !string.Equals(entry.OldFileHash, entry.NewFileHash, StringComparison.Ordinal);
                    entry.ClassificationChanged = !string.Equals(entry.OldClassificationTier, entry.NewClassificationTier, StringComparison.Ordinal);
                    entry.ProtectionChanged = entry.OldIsProtected != entry.NewIsProtected;
                    if (!entry.ContentChanged && !entry.ClassificationChanged && !entry.ProtectionChanged)
                    {
                        continue;
                    }
                }

                entries.Add(entry);
                previous = entry;
            }

            // Every file's timeline starts with its creation. If the earliest recorded row is already a
            // genesis row, that row is the creation. Otherwise the creation state is what the first real
            // change saw before it changed (its Old* values), or - when nothing ever changed - the file's
            // current state. Rows that predate Phase 1 carry no genesis, so the creation time falls back
            // to the earliest FirstSeenAtUtc on record.
            var firstEntry = entries.FirstOrDefault(e => e.EventType != "FullCopy");
            if (firstEntry == null || firstEntry.OldFileHash != null || firstEntry.OldClassificationTier != null)
            {
                var creation = firstEntry != null && firstEntry.OldFileHash != null
                    ? new FileVersionHistoryEntryDto
                    {
                        ObservedAtUtc = earliestSeenAtUtc,
                        NewFileHash = firstEntry.OldFileHash,
                        NewClassificationTier = firstEntry.OldClassificationTier ?? record.ClassificationTier,
                        NewIsProtected = firstEntry.OldIsProtected ?? record.IsProtected
                    }
                    : new FileVersionHistoryEntryDto
                    {
                        ObservedAtUtc = earliestSeenAtUtc,
                        NewFileHash = record.FileHash ?? string.Empty,
                        NewClassificationTier = record.ClassificationTier,
                        NewIsProtected = record.IsProtected
                    };
                entries.Insert(0, creation);
            }

            var sourceIds = entries.Where(e => e.SourceFileId.HasValue).Select(e => e.SourceFileId!.Value).Distinct().ToList();
            if (sourceIds.Count > 0)
            {
                var sources = await _db.FileInventoryRecords
                    .AsNoTracking()
                    .Where(x => sourceIds.Contains(x.FileId))
                    .Select(x => new { x.FileId, x.FileName, x.DeviceId })
                    .ToListAsync(cancellationToken);

                var sourceDeviceIds = sources.Select(s => s.DeviceId).Distinct().ToList();
                var sourceDeviceNames = await _db.Devices
                    .AsNoTracking()
                    .Where(d => sourceDeviceIds.Contains(d.Id))
                    .ToDictionaryAsync(d => d.Id, d => d.MachineName, cancellationToken);

                foreach (var entry in entries.Where(e => e.SourceFileId.HasValue))
                {
                    var source = sources.FirstOrDefault(s => s.FileId == entry.SourceFileId);
                    if (source == null) continue;

                    entry.SourceFileName = source.FileName;
                    entry.SourceDeviceId = source.DeviceId;
                    entry.SourceDeviceName = sourceDeviceNames.TryGetValue(source.DeviceId, out var sourceDeviceName) ? sourceDeviceName : null;
                }
            }

            return ApiResponse<FileVersionHistoryResultDto>.SuccessResponse(new FileVersionHistoryResultDto
            {
                FileId = fileId,
                FileName = record.FileName,
                DeviceId = record.DeviceId,
                EmployeeId = record.EmployeeId,
                Entries = entries
            });
        }

    }
}
