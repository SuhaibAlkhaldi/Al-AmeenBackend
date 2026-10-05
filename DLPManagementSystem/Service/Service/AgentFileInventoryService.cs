using System.Collections.Concurrent;
using System.Text.Json;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFileInventory;
using DLPManagementSystem.DTO.AgentFiles;
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

        // Two batches for the same device can overlap (live 2026-10-04: a downgrade rename's batch
        // committed its soft-delete ~26ms before a sibling batch's activeByPath snapshot/DB re-check
        // ran, so the sibling created a fresh, history-less FileId for the same path instead of
        // continuing the existing one). Serializing batches per device keeps every batch's snapshot
        // taken after the previous batch's commit. Process-wide because a DbContext is per request.
        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> DeviceBatchGates = new();

        public async Task<ApiResponse<AgentFileInventoryBatchResultDto>> ApplyFileInventoryBatchAsync(
            Guid organizationId,
            Guid deviceId,
            AgentFileInventoryBatchRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var gate = DeviceBatchGates.GetOrAdd(deviceId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await ApplyFileInventoryBatchCoreAsync(organizationId, deviceId, request, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<ApiResponse<AgentFileInventoryBatchResultDto>> ApplyFileInventoryBatchCoreAsync(
            Guid organizationId,
            Guid deviceId,
            AgentFileInventoryBatchRequestDto request,
            CancellationToken cancellationToken)
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

                // Phase 4 of the "File Tracking" plan: resolved once per batch (not per change) -
                // same synthetic-AuditEvent-then-Alert pattern DeviceStaleDetectionWorker already uses
                // for backend-detected conditions that aren't a real agent-reported permission
                // decision. "Error" for DecisionId and "High" for AlertLevel mirror that worker's own
                // choices exactly, for the same reason: this isn't a real Allow/Deny decision either.
                var downgradeEventTypeId = await _db.AuditEventTypes
                    .AsNoTracking()
                    .Where(x => x.Name == "FileClassificationDowngraded")
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var errorDecisionId = await _db.AuditDecisions
                    .AsNoTracking()
                    .Where(x => x.Name == "Error")
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var newAlertStatusId = await _db.AlertStatuses
                    .AsNoTracking()
                    .Where(x => x.Name == "New")
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var highAlertLevelId = await _db.AlertLevels
                    .AsNoTracking()
                    .Where(x => x.Name == "High")
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                DowngradeAlertLookups? downgradeAlertLookups = downgradeEventTypeId.HasValue && errorDecisionId.HasValue
                    && newAlertStatusId.HasValue && highAlertLevelId.HasValue
                    ? new DowngradeAlertLookups(downgradeEventTypeId.Value, errorDecisionId.Value, newAlertStatusId.Value, highAlertLevelId.Value)
                    : null;

                if (downgradeAlertLookups == null)
                {
                    _logger.LogWarning(
                        "Classification-downgrade alert lookup rows (AuditEventType/AuditDecision/AlertStatus/AlertLevel) are missing; downgrade detection will be skipped for this batch.");
                }

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

                // Tracks which target paths this exact batch has already written to (found live
                // 2026-09-27, see UpsertActiveRecordAsync's own comment on this set for the full
                // rationale) - distinct from activeByPath itself, which holds both this batch's own
                // fresh writes AND untouched pre-existing rows, and can't tell the two apart on its own.
                var pathsWrittenThisBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                            await UpsertActiveRecordAsync(activeByPath, pathsWrittenThisBatch, fileTypeMap, organizationId, deviceId, assignedEmployeeId, change, nowUtc, downgradeAlertLookups, cancellationToken);
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
                                IsSystemRewrite = change.IsSystemRewrite,
                                ContentFingerprint = change.ContentFingerprint,
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

                            // A rename always gets a brand-new FileId (see the comment above), but that
                            // does NOT mean it has no real "old" state to compare against - oldRecord
                            // (captured above, before it was soft-deleted) still holds the exact values
                            // this content had a moment ago under its previous path. Carrying them
                            // forward here is what lets Phase 1's history and Phase 4's downgrade check
                            // see a rename-driven reclassification (e.g. the classification-tagging
                            // feature renaming "[Secret] file.txt" to "[Public] file.txt") as the real
                            // transition it is, instead of a blind "Genesis" that hides the drop entirely.
                            var carriedOverPriorState = oldRecord != null
                                ? new PriorFileState(oldRecord.FileHash, oldRecord.ClassificationTier, oldRecord.IsProtected, oldRecord.ContentFingerprint)
                                : (PriorFileState?)null;

                            await UpsertActiveRecordAsync(activeByPath, pathsWrittenThisBatch, fileTypeMap, organizationId, deviceId, assignedEmployeeId, effectiveChange, nowUtc, downgradeAlertLookups, cancellationToken, carriedOverPriorState: carriedOverPriorState);
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
            catch (DbUpdateException exception) when (DbExceptionHelper.IsUniqueConstraintViolationOfIndex(exception, "UX_FileInventory_Active_DeviceId_FilePath"))
            {
                // Last-resort safety net for the residual, genuinely-simultaneous case
                // UpsertActiveRecordAsync's own re-check can't fully close (see its class comment) - a
                // concurrent batch for the same device won the race between that re-check and this
                // batch's own SaveChangesAsync. The whole batch fails cleanly here rather than a raw
                // exception surfacing as a generic 500; the agent's own outbox/retry already handles a
                // failed batch, and a retry will see the now-committed winner via a fresh activeByPath
                // load next time.
                _logger.LogWarning(exception,
                    "File inventory batch for organization {OrganizationId}, device {DeviceId} lost a concurrency race on an active-record path; the agent should retry.",
                    organizationId, deviceId);
                return ApiResponse<AgentFileInventoryBatchResultDto>.FailureResponse(
                    "A concurrent update for the same file path was applied first. Please retry this batch.",
                    "تم تطبيق تحديث متزامن لنفس مسار الملف أولًا. الرجاء إعادة إرسال هذه الدفعة");
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
        // it. Always checks activeByPath for change.FilePath (the TARGET path) first, regardless of
        // whether this is a Renamed change - a genuinely new FileId is only ever generated when no
        // active row exists yet for the target path (see FileInventoryRecord's class comment's
        // no-lineage-in-v1 design: the rename's OLD path's FileId specifically is still never reused,
        // that soft-delete happens separately in the caller before this runs).
        //
        // Duplicate-within-one-batch bug (found live 2026-09-27): an earlier version of this method
        // unconditionally skipped the activeByPath lookup for every Renamed change ("a rename always
        // gets a brand-new FileId"), reasoning that OK for a single rename but wrong for two Renamed
        // reports (observed for real: the agent reporting the same underlying rename twice) landing in
        // the SAME batch, targeting the SAME new path - the second one, blind to the first's own
        // in-memory activeByPath update, created a second, competing FileId with a blind "Genesis" (no
        // real prior classification), silently orphaning the first one's correctly-captured transition
        // onto a FileId that then lost the "which one stays active" pick. Checking activeByPath
        // unconditionally fixes this: the second report finds the first's already-applied record and
        // is correctly absorbed as a no-op (same target values, nothing left to change).
        //
        // Phase 1 of the "File Tracking" plan: also appends a FileVersionHistory row whenever this
        // upsert represents a genuine change - the file's first-ever appearance (a "Genesis" row, old
        // values null), or a real difference in hash/classification/protection versus what's *already
        // in the database* (record's current field values, read before they're overwritten below - the
        // database is the source of truth for "old", never a value asserted by the agent). A retried or
        // duplicate batch delivery that reproduces an already-applied state compares equal and writes no
        // new row. Both the FileInventoryRecords upsert and this history insert are staged on the same
        // DbContext and flushed together by the single SaveChangesAsync call at the end of
        // ApplyFileInventoryBatchAsync, so they commit atomically in one DB transaction.
        //
        // Race-condition safety net (found live 2026-09-27): activeByPath is a per-batch snapshot,
        // loaded once at the top of ApplyFileInventoryBatchAsync - it can be stale relative to a
        // *different*, concurrently-processing batch for the same device that has since committed its
        // own active row for this exact path (observed for real: an independent reconciliation-scan
        // report and a real-time rename report for the same underlying event, arriving in two
        // overlapping HTTP calls ~80ms apart). Before ever concluding "this path has no existing active
        // row" and creating a brand-new FileId, this re-checks directly against the database - not just
        // this batch's in-memory snapshot - so a concurrent winner is found and correctly treated as an
        // update, not raced against with a second, competing active row for the same path (which
        // UX_FileInventory_Active_DeviceId_FilePath exists specifically to forbid). The final
        // SaveChangesAsync in ApplyFileInventoryBatchAsync still catches that unique-index violation as
        // a last-resort safety net for the residual, genuinely-simultaneous case this recheck's own
        // read-then-act window cannot fully close.
        private async Task UpsertActiveRecordAsync(
            Dictionary<string, FileInventoryRecord> activeByPath,
            HashSet<string> pathsWrittenThisBatch,
            IReadOnlyDictionary<string, int> fileTypeMap,
            Guid organizationId,
            Guid deviceId,
            Guid? assignedEmployeeId,
            FileInventoryChangeDto change,
            DateTimeOffset nowUtc,
            DowngradeAlertLookups? downgradeAlertLookups,
            CancellationToken cancellationToken,
            PriorFileState? carriedOverPriorState = null)
        {
            // The agent sends an explicit Extension for a .dlpenc (its own path suffix is never the
            // real type - see FileInventoryChangeDto.Extension's comment); fall back to deriving it
            // from FilePath for every ordinary plaintext change, same as before this field existed.
            var extension = !string.IsNullOrWhiteSpace(change.Extension)
                ? change.Extension.Trim().TrimStart('.').ToLowerInvariant()
                : NormalizeExtension(change.FilePath);
            var fileTypeId = extension != null && fileTypeMap.TryGetValue(extension, out var mappedId) ? mappedId : (int?)null;

            var record = activeByPath.TryGetValue(change.FilePath, out var existing)
                ? existing
                : null;

            if (record == null)
            {
                // See this method's class comment - a fresh, targeted check for the concurrent-batch
                // race, not a trust in this batch's own (possibly stale) activeByPath snapshot.
                var concurrentlyCommittedRecord = await _db.FileInventoryRecords
                    .FirstOrDefaultAsync(x => x.DeviceId == deviceId && x.FilePath == change.FilePath && !x.IsDeleted, cancellationToken);

                if (concurrentlyCommittedRecord != null)
                {
                    record = concurrentlyCommittedRecord;
                }
            }

            // Snapshot of what's currently in the database, taken BEFORE any field below is overwritten -
            // this is the "old" side of the history comparison, per the DB-as-source-of-truth design.
            // carriedOverPriorState (a rename's renamed-from row's own values, captured by the caller
            // before it was soft-deleted) takes precedence over whatever `record` itself shows, UNLESS
            // `record`'s own path was already written earlier in THIS SAME BATCH (pathsWrittenThisBatch) -
            // in that case `record`'s current values are strictly fresher than carriedOverPriorState's,
            // which only knows about the renamed-from side and has no idea another change already moved
            // this exact target path forward moments ago in this same loop.
            //
            // Same-target-converging-renames bug (found live 2026-09-27, immediately after the
            // suhaib2.txt File Tracking retest): two real, DIFFERENT renames landing in ONE batch and
            // converging on the SAME final path (observed live: the classification-tagging rename and
            // the content-watermarker's own tmp-swap rename, both ending at "[Secret] file.txt", arriving
            // in whichever order the agent's outbox happened to flush them) used to let whichever one was
            // processed SECOND blindly apply its OWN carriedOverPriorState (its own renamed-from row's
            // values) as "old", even when `record` at the target path had ALREADY been updated to a newer
            // state by the FIRST one's processing earlier in this very loop - silently regressing the
            // record's fields backward with no history row flagging it (the stale-vs-fresher values
            // compared equal), or spawning a phantom extra "Genesis" FileId that orphaned the real chain.
            // Checking pathsWrittenThisBatch first fixes this: once a target path has been written by an
            // earlier change in this batch, every later change to that same path is treated as a
            // continuation of what THIS batch already applied to it, never as a blind replay of a
            // renamed-from row that is now stale.
            //
            // This does NOT weaken Bug #3's original fix (cross-batch/concurrent-race carriedOverPriorState
            // precedence): that scenario's `record` is found via the DB re-check just above for a path
            // this batch has not itself written yet, so pathsWrittenThisBatch does not contain it and
            // carriedOverPriorState still wins, exactly as before.
            var priorState = (record != null && pathsWrittenThisBatch.Contains(change.FilePath))
                ? new PriorFileState(record.FileHash, record.ClassificationTier, record.IsProtected, record.ContentFingerprint)
                : carriedOverPriorState
                    ?? (record != null ? new PriorFileState(record.FileHash, record.ClassificationTier, record.IsProtected, record.ContentFingerprint) : null);

            var isGenuineGenesis = priorState == null;
            var oldFileHash = priorState?.FileHash;
            var oldClassificationTier = priorState?.ClassificationTier;
            var oldIsProtected = priorState?.IsProtected;

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
            record.ContentFingerprint = change.ContentFingerprint;
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
            pathsWrittenThisBatch.Add(change.FilePath);

            AppendVersionHistoryIfChanged(
                organizationId, deviceId, assignedEmployeeId, change.OccurredAtUtc, record,
                isGenuineGenesis, change.IsSystemRewrite == true, IsContentTextChanged(priorState?.ContentFingerprint, change.ContentFingerprint, change.IsSystemRewrite == true), oldFileHash, oldClassificationTier, oldIsProtected, downgradeAlertLookups);

            await RecordFullCopyIfDetectedAsync(organizationId, deviceId, assignedEmployeeId, record, change.OccurredAtUtc, cancellationToken);
        }

        // Records a FullCopy event on the file whose content just became identical to another active file's
        // (the one that changed is the copy; the earliest-created match is the source). Not decided by which
        // file was created first, because a file created later can be the one that received the copy. The
        // event is stored once per (copy, source) pair, so it survives later edits to either file.
        private async Task RecordFullCopyIfDetectedAsync(
            Guid organizationId,
            Guid deviceId,
            Guid? assignedEmployeeId,
            FileInventoryRecord record,
            DateTimeOffset observedAtUtc,
            CancellationToken cancellationToken)
        {
            if (record.IsDeleted) return;
            if (string.IsNullOrEmpty(record.ContentFingerprint) && string.IsNullOrEmpty(record.FileHash)) return;

            var fingerprint = record.ContentFingerprint;
            var hash = record.FileHash;

            var source = await _db.FileInventoryRecords
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && !x.IsDeleted && x.FileId != record.FileId
                    && (fingerprint != null ? x.ContentFingerprint == fingerprint : x.FileHash == hash))
                .OrderBy(x => x.FirstSeenAtUtc)
                .Select(x => new { x.FileId })
                .FirstOrDefaultAsync(cancellationToken);

            if (source == null) return;

            var alreadyRecorded = await _db.FileVersionHistories
                .AnyAsync(x => x.FileId == record.FileId && x.EventType == "FullCopy" && x.SourceFileId == source.FileId, cancellationToken);
            if (alreadyRecorded) return;

            _db.FileVersionHistories.Add(new FileVersionHistory
            {
                Id = Guid.NewGuid(),
                FileId = record.FileId,
                OrganizationId = organizationId,
                ObservedAtUtc = observedAtUtc,
                NewFileHash = record.FileHash ?? string.Empty,
                NewContentFingerprint = record.ContentFingerprint,
                NewClassificationTier = record.ClassificationTier,
                NewIsProtected = record.IsProtected,
                EventType = "FullCopy",
                SourceFileId = source.FileId,
                DeviceId = deviceId,
                EmployeeId = assignedEmployeeId
            });
        }

        // True when the file's visible text differs from what was recorded before. A system rewrite on a record
        // that predates fingerprints cannot be compared, so it is treated as the one-time fingerprint backfill
        // it almost always is - recording it would attribute a restart's re-watermarking to whoever holds the
        // device. Any other unknown is still treated as a change.
        private static bool IsContentTextChanged(string? previousFingerprint, string? currentFingerprint, bool isSystemRewrite)
        {
            if (previousFingerprint == null && isSystemRewrite) return false;
            return previousFingerprint == null || currentFingerprint == null
                || !string.Equals(previousFingerprint, currentFingerprint, StringComparison.Ordinal);
        }

        // Compares the prior state (captured by the caller before anything was overwritten - either
        // the existing record's own values, or a renamed-from row's carried-over values; see
        // UpsertActiveRecord) against the newly-applied ones on `record`, and appends one
        // FileVersionHistory row if - and only if - a genuine change occurred (see this method's
        // callers' comment for the full rationale). isGenuineGenesis is true only when there is truly
        // no prior state to compare against (a path never seen before, on any FileId) - a rename with
        // no resolvable predecessor is the one other case this can happen, otherwise a rename's prior
        // state is its renamed-from row's, not null.
        //
        // Also implements Phase 4: the moment a real classification downgrade is observed - never on a
        // genuine Genesis row, which has no real prior tier to have downgraded from - it raises an
        // immediate Alert, in this same call, same DbContext, same transaction as everything above,
        // rather than waiting on a periodic worker. Because a rename now carries its real prior state
        // forward (see above), this correctly fires for a rename-driven reclassification too - e.g. the
        // classification-tagging feature renaming "[Secret] file.txt" to "[Public] file.txt".
        private void AppendVersionHistoryIfChanged(
            Guid organizationId,
            Guid deviceId,
            Guid? assignedEmployeeId,
            DateTimeOffset observedAtUtc,
            FileInventoryRecord record,
            bool isGenuineGenesis,
            bool isSystemRewrite,
            bool contentTextChanged,
            string? oldFileHash,
            string? oldClassificationTier,
            bool? oldIsProtected,
            DowngradeAlertLookups? downgradeAlertLookups)
        {
            var contentChanged = isGenuineGenesis || !string.Equals(oldFileHash, record.FileHash, StringComparison.Ordinal);
            var classificationChanged = isGenuineGenesis || !string.Equals(oldClassificationTier, record.ClassificationTier, StringComparison.Ordinal);
            var protectionChanged = isGenuineGenesis || oldIsProtected != record.IsProtected;

            if (!contentChanged && !classificationChanged && !protectionChanged)
            {
                return;
            }

            // The agent's own watermark rewrite changes the bytes but not what the file is classified as
            // or whether it is protected - nobody modified the file, so it is not recorded as a change.
            // A rename-created row with no predecessor state is still just the agent's rewrite of content
            // it already knew about, so it's skipped too; the file's creation is recorded by its first row.
            if (isSystemRewrite && !contentTextChanged && (isGenuineGenesis || (!classificationChanged && !protectionChanged)))
            {
                return;
            }

            _db.FileVersionHistories.Add(new FileVersionHistory
            {
                Id = Guid.NewGuid(),
                FileId = record.FileId,
                OrganizationId = organizationId,
                ObservedAtUtc = observedAtUtc,
                OldFileHash = isGenuineGenesis ? null : oldFileHash,
                NewFileHash = record.FileHash ?? string.Empty,
                NewContentFingerprint = record.ContentFingerprint,
                OldClassificationTier = isGenuineGenesis ? null : oldClassificationTier,
                NewClassificationTier = record.ClassificationTier,
                OldIsProtected = isGenuineGenesis ? null : oldIsProtected,
                NewIsProtected = record.IsProtected,
                ContentChanged = contentChanged,
                ClassificationChanged = classificationChanged,
                ProtectionChanged = protectionChanged,
                DeviceId = deviceId,
                EmployeeId = assignedEmployeeId
            });

            if (!isGenuineGenesis && classificationChanged && downgradeAlertLookups.HasValue
                && IsClassificationDowngrade(oldClassificationTier, record.ClassificationTier))
            {
                RaiseClassificationDowngradeAlert(
                    organizationId, deviceId, assignedEmployeeId, observedAtUtc, record,
                    oldClassificationTier!, downgradeAlertLookups.Value);
            }
        }

        // Higher index = more sensitive (see ClassificationTiers.Order) - a downgrade is any move to a
        // strictly lower index. Rises in sensitivity, and changes where either tier isn't recognized,
        // are deliberately not alerted on (see Phase 4's plan section for why only the downgrade
        // direction is alert-worthy).
        private static bool IsClassificationDowngrade(string? oldTier, string newTier)
        {
            if (oldTier == null)
            {
                return false;
            }

            var oldRank = ClassificationTiers.Order.ToList().IndexOf(oldTier);
            var newRank = ClassificationTiers.Order.ToList().IndexOf(newTier);
            return oldRank >= 0 && newRank >= 0 && newRank < oldRank;
        }

        private void RaiseClassificationDowngradeAlert(
            Guid organizationId,
            Guid deviceId,
            Guid? employeeId,
            DateTimeOffset observedAtUtc,
            FileInventoryRecord record,
            string oldClassificationTier,
            DowngradeAlertLookups lookups)
        {
            var metadata = JsonSerializer.Serialize(new
            {
                fileId = record.FileId,
                filePath = record.FilePath,
                fileName = record.FileName,
                oldClassificationTier,
                newClassificationTier = record.ClassificationTier,
                deviceId,
                employeeId
            });

            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DeviceId = deviceId,
                EmployeeId = employeeId,
                ActionKey = PermissionActionKeys.FileInventoryMonitor,
                EventTypeId = lookups.EventTypeId,
                DecisionId = lookups.DecisionId,
                OccurredAtUtc = observedAtUtc,
                ReceivedAtUtc = DateTimeOffset.UtcNow,
                MetadataJson = metadata
            };
            _db.AuditEvents.Add(auditEvent);

            _db.Alerts.Add(new Alert
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AuditEventId = auditEvent.Id,
                AlertLevelId = lookups.AlertLevelId,
                AlertStatusId = lookups.AlertStatusId,
                Title = $"Classification downgraded — {record.FileName}",
                Description =
                    $"File '{record.FileName}' ({record.FilePath}) had its classification lowered " +
                    $"from {oldClassificationTier} to {record.ClassificationTier}. This may indicate " +
                    "protection being deliberately removed.",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        // Phase 4/5 shared lookup bundle - resolved once per batch (ApplyFileInventoryBatchAsync), not
        // once per change, since none of these rows ever vary within a single batch.
        private readonly record struct DowngradeAlertLookups(int EventTypeId, int DecisionId, int AlertStatusId, int AlertLevelId);

        // A FileInventoryRecord's hash/classification/protection at some prior moment - either the
        // record's own current values (an active row already found for the target path) or a
        // renamed-from row's values carried forward by the caller (Renamed, no active row found yet
        // for the target path - see the Renamed case in ApplyFileInventoryBatchAsync). Letting a
        // rename supply this explicitly, rather than treating "no active row at the target path yet"
        // as "no prior state exists", is what makes a rename-driven reclassification (e.g. the
        // classification-tagging feature's "[Secret] file.txt" -> "[Public] file.txt") show up as a
        // real transition instead of a blind Genesis that hides it.
        private readonly record struct PriorFileState(string? FileHash, string? ClassificationTier, bool? IsProtected, string? ContentFingerprint);

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
