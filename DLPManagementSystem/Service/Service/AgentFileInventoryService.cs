using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFileInventory;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    // Applies a batch of file-inventory changes reported by an agent's FileInventorySyncWorker.
    //
    // Unlike AgentAuditEventService (an append-only log - the same event id sent twice is a genuine
    // duplicate insert that must be skipped), FileInventoryRecords is a *current-state* table: every
    // change here is applied through an idempotent upsert/soft-delete, so re-delivering an
    // already-applied change (e.g. the agent retries a batch whose response never arrived) just
    // re-produces the exact same end state rather than corrupting anything. That's why there is no
    // persisted ChangeId dedup ledger and no "duplicate" outcome bucket in the response - see
    // AgentFileInventoryBatchResultDto's class comment.
    public class AgentFileInventoryService : IAgentFileInventoryService
    {
        private const int MaxChangesPerBatch = 500;

        private readonly DLPSystemContext _db;
        private readonly ILogger<AgentFileInventoryService> _logger;

        public AgentFileInventoryService(DLPSystemContext db, ILogger<AgentFileInventoryService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ApiResponse<AgentFileInventoryBatchResultDto>> ApplyFileInventoryBatchAsync(
            Guid organizationId,
            Guid deviceId,
            AgentFileInventoryBatchRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.TenantId != organizationId || request.DeviceId != deviceId)
                {
                    return ApiResponse<AgentFileInventoryBatchResultDto>.FailureResponse(
                        "tenantId/deviceId do not match the authenticated device.",
                        "معرّف المؤسسة أو الجهاز لا يطابق الجهاز المصادق عليه");
                }

                if (request.Changes == null || request.Changes.Count == 0)
                {
                    return ApiResponse<AgentFileInventoryBatchResultDto>.FailureResponse(
                        "At least one file inventory change is required.",
                        "يجب إرسال تغيير واحد على الأقل");
                }

                if (request.Changes.Count > MaxChangesPerBatch)
                {
                    return ApiResponse<AgentFileInventoryBatchResultDto>.FailureResponse(
                        $"A batch cannot contain more than {MaxChangesPerBatch} changes.",
                        $"لا يمكن أن تحتوي الدفعة على أكثر من {MaxChangesPerBatch} تغيير");
                }

                var device = await _db.Devices
                    .FirstOrDefaultAsync(x => x.Id == deviceId && x.OrganizationId == organizationId, cancellationToken);

                if (device == null)
                {
                    return ApiResponse<AgentFileInventoryBatchResultDto>.FailureResponse(
                        "Device was not found.",
                        "الجهاز غير مسجل في النظام");
                }

                var isReconciliation = string.Equals(request.SyncKind, FileInventorySyncKinds.Reconciliation, StringComparison.OrdinalIgnoreCase);

                // Same fallback this system already uses for AuditEvents: EmployeeWindowsIdentities is
                // never actually populated today, so the device's currently-assigned employee
                // (DeviceUserAssignments) is the realistic source of truth. Resolved once per batch, not
                // per change - every change in one batch is about the same device, so the owning
                // employee doesn't vary within it.
                var assignedEmployeeId = await _db.DeviceUserAssignments
                    .AsNoTracking()
                    .Where(x => x.DeviceId == deviceId && x.UnassignedAtUtc == null)
                    .Select(x => (Guid?)x.EmployeeId)
                    .FirstOrDefaultAsync(cancellationToken);

                var fileTypeMap = await _db.FileTypes
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Extension, x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var touchedPaths = request.Changes
                    .SelectMany(x => new[] { x.FilePath, x.OldFilePath })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Loaded once up front, then kept up to date in memory as changes are applied below -
                // multiple changes in the same batch can touch the same path (e.g. Created then Modified
                // for a file that changed twice between sync ticks), and each must see the previous
                // change's effect, not stale data from before the batch started.
                var activeByPath = await _db.FileInventoryRecords
                    .Where(x => x.DeviceId == deviceId && !x.IsDeleted && touchedPaths.Contains(x.FilePath))
                    .ToDictionaryAsync(x => x.FilePath, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var result = new AgentFileInventoryBatchResultDto();
                var nowUtc = DateTimeOffset.UtcNow;

                foreach (var change in request.Changes)
                {
                    if (string.IsNullOrWhiteSpace(change.FilePath))
                    {
                        result.RejectedChanges.Add(new RejectedFileInventoryChangeDto
                        {
                            ChangeId = change.ChangeId,
                            ReasonCode = "MissingFilePath",
                            Retryable = false
                        });
                        continue;
                    }

                    switch (change.ChangeType)
                    {
                        case FileInventoryChangeTypes.Created:
                        case FileInventoryChangeTypes.Modified:
                        {
                            if (change.SizeBytes is null || string.IsNullOrWhiteSpace(change.ClassificationTier)
                                || string.IsNullOrWhiteSpace(change.Provenance))
                            {
                                result.RejectedChanges.Add(new RejectedFileInventoryChangeDto
                                {
                                    ChangeId = change.ChangeId,
                                    ReasonCode = "MissingRequiredFields",
                                    Retryable = false
                                });
                                continue;
                            }

                            UpsertActiveRecord(activeByPath, fileTypeMap, organizationId, deviceId, assignedEmployeeId, change, nowUtc);
                            result.AcceptedChangeIds.Add(change.ChangeId);
                            break;
                        }

                        case FileInventoryChangeTypes.Renamed:
                        {
                            if (string.IsNullOrWhiteSpace(change.OldFilePath))
                            {
                                result.RejectedChanges.Add(new RejectedFileInventoryChangeDto
                                {
                                    ChangeId = change.ChangeId,
                                    ReasonCode = "MissingOldFilePath",
                                    Retryable = false
                                });
                                continue;
                            }

                            // No-lineage-in-v1 design (see FileInventoryRecord's class comment): the old
                            // row is soft-deleted, a brand-new row (new FileId) is upserted for the new
                            // path - never a rename-in-place of the same FileId. If OldFilePath has
                            // already been soft-deleted (retry, or an out-of-order Reconciliation change),
                            // this is simply a no-op for that half of the operation, not an error.
                            if (activeByPath.TryGetValue(change.OldFilePath, out var oldRecord))
                            {
                                oldRecord.IsDeleted = true;
                                oldRecord.DeletedAtUtc = nowUtc;
                                oldRecord.UpdatedAtUtc = nowUtc;
                                activeByPath.Remove(change.OldFilePath);
                            }

                            // Fields are optional on a Renamed change (a pure move/rename carries the
                            // previous classification/size/hash forward); fall back to the old row's
                            // values when the agent didn't resend them.
                            var effectiveChange = new FileInventoryChangeDto
                            {
                                ChangeId = change.ChangeId,
                                ChangeType = change.ChangeType,
                                FilePath = change.FilePath,
                                FileHash = change.FileHash ?? oldRecord?.FileHash,
                                Extension = change.Extension ?? oldRecord?.Extension,
                                SizeBytes = change.SizeBytes ?? oldRecord?.SizeBytes,
                                ClassificationTier = change.ClassificationTier ?? oldRecord?.ClassificationTier,
                                Provenance = change.Provenance ?? oldRecord?.Provenance,
                                IsProtected = change.IsProtected ?? oldRecord?.IsProtected,
                                OccurredAtUtc = change.OccurredAtUtc
                            };

                            if (effectiveChange.SizeBytes is null || string.IsNullOrWhiteSpace(effectiveChange.ClassificationTier)
                                || string.IsNullOrWhiteSpace(effectiveChange.Provenance))
                            {
                                result.RejectedChanges.Add(new RejectedFileInventoryChangeDto
                                {
                                    ChangeId = change.ChangeId,
                                    ReasonCode = "MissingRequiredFields",
                                    Retryable = false
                                });
                                continue;
                            }

                            UpsertActiveRecord(activeByPath, fileTypeMap, organizationId, deviceId, assignedEmployeeId, effectiveChange, nowUtc, forceNewFileId: true);
                            result.AcceptedChangeIds.Add(change.ChangeId);
                            break;
                        }

                        case FileInventoryChangeTypes.Deleted:
                        {
                            // A path with no active row (already deleted, or never synced) is treated as
                            // already-satisfied rather than rejected - this is the one behavioral fork
                            // SyncKind exists for: Reconciliation batches routinely re-report deletions
                            // an earlier Incremental batch already applied.
                            if (activeByPath.TryGetValue(change.FilePath, out var record))
                            {
                                record.IsDeleted = true;
                                record.DeletedAtUtc = nowUtc;
                                record.UpdatedAtUtc = nowUtc;
                                activeByPath.Remove(change.FilePath);
                            }
                            else if (!isReconciliation)
                            {
                                // Outside of reconciliation, a Deleted for a path the backend never had
                                // active is unusual (missed Created, or client/server state genuinely
                                // diverged) - still accepted (nothing to do IS the correct end state),
                                // just worth a log line to notice if it happens a lot.
                                _logger.LogDebug(
                                    "FileInventory Deleted change for a path with no active record (device {DeviceId}, path {FilePath}, syncKind {SyncKind}).",
                                    deviceId, change.FilePath, request.SyncKind);
                            }

                            result.AcceptedChangeIds.Add(change.ChangeId);
                            break;
                        }

                        default:
                            result.RejectedChanges.Add(new RejectedFileInventoryChangeDto
                            {
                                ChangeId = change.ChangeId,
                                ReasonCode = "UnknownChangeType",
                                Retryable = false
                            });
                            break;
                    }
                }

                device.LastSeenAtUtc = nowUtc;
                device.AgentVersion = request.AgentVersion;
                device.UpdatedAtUtc = nowUtc;

                await _db.SaveChangesAsync(cancellationToken);

                return ApiResponse<AgentFileInventoryBatchResultDto>.SuccessResponse(
                    result,
                    "File inventory batch processed.",
                    "تمت معالجة دفعة جرد الملفات");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Unexpected error while applying a file inventory batch for organization {OrganizationId}, device {DeviceId}.",
                    organizationId, deviceId);
                return ApiResponse<AgentFileInventoryBatchResultDto>.FailureResponse(
                    "Unexpected error occurred while applying file inventory changes.",
                    "حدث خطأ غير متوقع أثناء تطبيق تغييرات جرد الملفات");
            }
        }

        // Finds-or-creates the single active row for change.FilePath and applies the change's values to
        // it. forceNewFileId=true (Renamed) skips reusing an existing row even if one happens to already
        // be tracked for that path - a rename always produces a new FileId, per the no-lineage-in-v1
        // design (see FileInventoryRecord's class comment); Created/Modified update the existing row for
        // the same device+path in place if one exists, since that's the same file being re-scanned.
        private void UpsertActiveRecord(
            Dictionary<string, FileInventoryRecord> activeByPath,
            IReadOnlyDictionary<string, int> fileTypeMap,
            Guid organizationId,
            Guid deviceId,
            Guid? assignedEmployeeId,
            FileInventoryChangeDto change,
            DateTimeOffset nowUtc,
            bool forceNewFileId = false)
        {
            // The agent sends an explicit Extension for a .dlpenc (its own path suffix is never the
            // real type - see FileInventoryChangeDto.Extension's comment); fall back to deriving it
            // from FilePath for every ordinary plaintext change, same as before this field existed.
            var extension = !string.IsNullOrWhiteSpace(change.Extension)
                ? change.Extension.Trim().TrimStart('.').ToLowerInvariant()
                : NormalizeExtension(change.FilePath);
            var fileTypeId = extension != null && fileTypeMap.TryGetValue(extension, out var mappedId) ? mappedId : (int?)null;

            var record = !forceNewFileId && activeByPath.TryGetValue(change.FilePath, out var existing)
                ? existing
                : null;

            if (record == null)
            {
                record = new FileInventoryRecord
                {
                    FileId = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    DeviceId = deviceId,
                    FirstSeenAtUtc = nowUtc
                };
                _db.FileInventoryRecords.Add(record);
            }

            record.EmployeeId = assignedEmployeeId;
            record.FilePath = change.FilePath;
            record.FileName = System.IO.Path.GetFileName(change.FilePath);
            record.FileHash = change.FileHash;
            record.FileTypeId = fileTypeId;
            record.Extension = extension;
            record.SizeBytes = change.SizeBytes!.Value;
            record.ClassificationTier = change.ClassificationTier!;
            record.Provenance = change.Provenance!;
            record.IsProtected = change.IsProtected ?? false;
            record.LastScanAtUtc = change.OccurredAtUtc;
            record.UpdatedAtUtc = nowUtc;
            record.IsDeleted = false;
            record.DeletedAtUtc = null;

            activeByPath[change.FilePath] = record;
        }

        private static string? NormalizeExtension(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            return extension.TrimStart('.').ToLowerInvariant();
        }
    }
}
