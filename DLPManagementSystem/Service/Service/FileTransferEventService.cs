using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileTransfer;
using DLPManagementSystem.Helper.FileTransfer;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class FileTransferEventService : IFileTransferEventService
    {
        private const int MaxBatchEvents = 500;

        // Only Gmail is implemented in this step. Other channels are added one at a time, each with its own extension support.
        private static readonly HashSet<string> AllowedChannels = new(StringComparer.Ordinal) { "GMAIL" };
        private static readonly HashSet<string> AllowedRecipientKinds = new(StringComparer.Ordinal) { "EMAIL", "CHAT_NAME", "GROUP_NAME", "UNKNOWN" };
        private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal) { "TO", "CC" };

        // An observation is only believable if its recipient evidence type belongs to its recipient kind.
        private static readonly Dictionary<string, HashSet<string>> EvidenceByKind = new(StringComparer.Ordinal)
        {
            ["EMAIL"] = new(StringComparer.Ordinal) { FileTransferEvidenceRules.RecipientEvidence_EmailInSendForm, FileTransferEvidenceRules.RecipientEvidence_EmailPartialRead },
            ["CHAT_NAME"] = new(StringComparer.Ordinal) { FileTransferEvidenceRules.RecipientEvidence_TeamsChatName, FileTransferEvidenceRules.RecipientEvidence_ChatContactName },
            ["GROUP_NAME"] = new(StringComparer.Ordinal) { FileTransferEvidenceRules.RecipientEvidence_GroupName }
        };

        private readonly DLPSystemContext _db;
        private readonly ILogger<FileTransferEventService> _logger;

        public FileTransferEventService(DLPSystemContext db, ILogger<FileTransferEventService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ApiResponse<FileTransferEventBatchResultDto>> IngestAsync(
            Guid organizationId,
            Guid deviceId,
            FileTransferEventBatchRequestDto request,
            CancellationToken cancellationToken)
        {
            // The batch body must describe the same device and organization the token was issued for.
            if (request.TenantId != organizationId || request.DeviceId != deviceId)
            {
                return ApiResponse<FileTransferEventBatchResultDto>.FailureResponse(
                    "Batch identity does not match the device token.",
                    "هوية الدفعة لا تطابق توكن الجهاز");
            }

            if (request.Events.Count > MaxBatchEvents)
            {
                return ApiResponse<FileTransferEventBatchResultDto>.FailureResponse(
                    $"A batch can contain at most {MaxBatchEvents} events.",
                    $"الدفعة لا يمكن أن تتجاوز {MaxBatchEvents} حدثاً");
            }

            var deviceExists = await _db.Devices
                .AsNoTracking()
                .AnyAsync(x => x.Id == deviceId && x.OrganizationId == organizationId, cancellationToken);

            if (!deviceExists)
            {
                return ApiResponse<FileTransferEventBatchResultDto>.FailureResponse(
                    "Device was not found.",
                    "الجهاز غير موجود");
            }

            // Resolved once per batch, like the file inventory: every observation in a batch comes from the same device.
            var assignedEmployeeId = await _db.DeviceUserAssignments
                .AsNoTracking()
                .Where(x => x.DeviceId == deviceId && x.UnassignedAtUtc == null)
                .Select(x => (Guid?)x.EmployeeId)
                .FirstOrDefaultAsync(cancellationToken);

            var sendActionIds = request.Events.Select(x => x.SendActionId).Distinct().ToList();
            var alreadyStored = (await _db.FileTransferEvents
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && sendActionIds.Contains(x.SendActionId))
                .Select(x => new { x.SendActionId, x.RecipientOrdinal })
                .ToListAsync(cancellationToken))
                .Select(x => (x.SendActionId, x.RecipientOrdinal))
                .ToHashSet();

            var result = new FileTransferEventBatchResultDto();
            var receivedAtUtc = DateTimeOffset.UtcNow;

            foreach (var observation in request.Events)
            {
                if (!TryNormalize(observation, receivedAtUtc, out var normalized))
                {
                    result.RejectedInvalid++;
                    continue;
                }

                if (!alreadyStored.Add((observation.SendActionId, observation.RecipientOrdinal)))
                {
                    result.Duplicates++;
                    continue;
                }

                var candidates = await LoadCandidatesAsync(organizationId, deviceId, normalized, cancellationToken);
                var correlation = FileTransferCorrelator.Correlate(
                    new FileObservation(normalized.FileName, normalized.FileSizeBytes, normalized.FileHash, normalized.ContentFingerprint, normalized.ObservedAtUtc),
                    candidates);

                if (correlation.FileMatch == FileTransferEvidenceRules.FileMatch_NoMatch)
                {
                    result.RejectedNoFileMatch++;
                    continue;
                }

                if (!FileTransferEvidenceRules.IsKnownTier(correlation.ClassificationTier))
                {
                    result.RejectedUnknownClassification++;
                    continue;
                }

                var fileLevel = FileTransferEvidenceRules.FileLevel(correlation.FileMatch);
                var recipientLevel = FileTransferEvidenceRules.RecipientLevel(normalized.RecipientEvidenceType);
                var overall = FileTransferEvidenceRules.Overall(fileLevel, recipientLevel);
                if (overall == FileTransferEvidenceRules.Level_None)
                {
                    result.RejectedInvalid++;
                    continue;
                }

                _db.FileTransferEvents.Add(new FileTransferEvent
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    SendActionId = normalized.SendActionId,
                    RecipientOrdinal = normalized.RecipientOrdinal,
                    SourceDeviceId = deviceId,
                    SourceEmployeeId = assignedEmployeeId,
                    Channel = normalized.Channel,
                    RecipientRole = normalized.RecipientRole,
                    RecipientKind = normalized.RecipientKind,
                    RecipientValue = normalized.RecipientValue,
                    RecipientEvidenceType = normalized.RecipientEvidenceType,
                    FileId = correlation.FileId,
                    ContentFingerprint = correlation.ContentFingerprint ?? normalized.ContentFingerprint,
                    FileHashBytes = normalized.FileHash,
                    FileName = normalized.FileName,
                    FileSizeBytes = normalized.FileSizeBytes,
                    ClassificationTierAtEvent = correlation.ClassificationTier!,
                    FileEvidenceType = correlation.FileMatch,
                    FileEvidenceLevel = fileLevel,
                    RecipientEvidenceLevel = recipientLevel,
                    OverallEvidenceLevel = overall,
                    ObservedAtUtc = normalized.ObservedAtUtc,
                    ReceivedAtUtc = receivedAtUtc,
                    ExtensionVersion = normalized.ExtensionVersion,
                    CorrelationRuleVersion = FileTransferEvidenceRules.RuleVersion
                });
                result.Accepted++;
            }

            if (result.Accepted > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            // Counts only. Recipient and file values are never logged.
            _logger.LogInformation(
                "File transfer batch from device {DeviceId}: accepted {Accepted}, duplicates {Duplicates}, invalid {Invalid}, no file match {NoMatch}, unknown classification {UnknownClassification}.",
                deviceId, result.Accepted, result.Duplicates, result.RejectedInvalid, result.RejectedNoFileMatch, result.RejectedUnknownClassification);

            return ApiResponse<FileTransferEventBatchResultDto>.SuccessResponse(result);
        }

        public async Task<ApiResponse<PagedResultDto<FileTransferEventListItemDto>>> ListAsync(
            Guid organizationId,
            Guid? fileId,
            Guid? employeeId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            page = Math.Max(1, page);
            pageSize = PagingDefaults.ClampPageSize(pageSize);

            var query = _db.FileTransferEvents
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            if (fileId.HasValue) query = query.Where(x => x.FileId == fileId.Value);
            if (employeeId.HasValue) query = query.Where(x => x.SourceEmployeeId == employeeId.Value);
            if (fromUtc.HasValue) query = query.Where(x => x.ObservedAtUtc >= fromUtc.Value);
            if (toUtc.HasValue) query = query.Where(x => x.ObservedAtUtc <= toUtc.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var pageRows = await query
                .OrderByDescending(x => x.ObservedAtUtc)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    Event = x,
                    DeviceName = x.SourceDevice.MachineName
                })
                .ToListAsync(cancellationToken);

            // Employee names come from a separate lookup, so the event does not need a navigation property to Employee.
            var employeeIds = pageRows
                .Where(x => x.Event.SourceEmployeeId != null)
                .Select(x => x.Event.SourceEmployeeId!.Value)
                .Distinct()
                .ToList();

            var employeeNames = employeeIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Employees
                    .AsNoTracking()
                    .Where(x => x.OrganizationId == organizationId && employeeIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

            var rows = pageRows.Select(row => new FileTransferEventListItemDto
            {
                Id = row.Event.Id,
                SendActionId = row.Event.SendActionId,
                RecipientOrdinal = row.Event.RecipientOrdinal,
                ObservedAtUtc = row.Event.ObservedAtUtc,
                SourceEmployeeName = row.Event.SourceEmployeeId is Guid employeeId && employeeNames.TryGetValue(employeeId, out var name) ? name : null,
                SourceDeviceName = row.DeviceName,
                Channel = row.Event.Channel,
                RecipientRole = row.Event.RecipientRole,
                RecipientKind = row.Event.RecipientKind,
                RecipientValue = row.Event.RecipientValue,
                FileId = row.Event.FileId,
                FileName = row.Event.FileName,
                FileSizeBytes = row.Event.FileSizeBytes,
                ClassificationTierAtEvent = row.Event.ClassificationTierAtEvent,
                FileEvidenceType = row.Event.FileEvidenceType,
                FileEvidenceLevel = row.Event.FileEvidenceLevel,
                RecipientEvidenceType = row.Event.RecipientEvidenceType,
                RecipientEvidenceLevel = row.Event.RecipientEvidenceLevel,
                OverallEvidenceLevel = row.Event.OverallEvidenceLevel,
                CorrelationRuleVersion = row.Event.CorrelationRuleVersion
            }).ToList();

            return ApiResponse<PagedResultDto<FileTransferEventListItemDto>>.SuccessResponse(new PagedResultDto<FileTransferEventListItemDto>
            {
                Items = rows,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        private async Task<List<CorrelationCandidate>> LoadCandidatesAsync(
            Guid organizationId,
            Guid deviceId,
            NormalizedObservation observation,
            CancellationToken cancellationToken)
        {
            // Same device only. The three conditions are the three evidence paths: byte hash, fingerprint, name + size.
            var hash = observation.FileHash;
            var fingerprint = observation.ContentFingerprint;
            var name = observation.FileName;
            var size = observation.FileSizeBytes;

            return await _db.FileInventoryRecords
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.DeviceId == deviceId
                    && ((hash != null && x.FileHash == hash)
                        || (fingerprint != null && x.ContentFingerprint == fingerprint)
                        || (x.FileName == name && x.SizeBytes == size)))
                .Select(x => new CorrelationCandidate(
                    x.FileId,
                    x.FileHash,
                    x.ContentFingerprint,
                    x.FileName,
                    x.SizeBytes,
                    x.ClassificationTier,
                    x.LastScanAtUtc))
                .ToListAsync(cancellationToken);
        }

        private static bool TryNormalize(FileTransferObservationDto observation, DateTimeOffset receivedAtUtc, out NormalizedObservation normalized)
        {
            normalized = null!;

            if (observation.SendActionId == Guid.Empty || observation.RecipientOrdinal < 0) return false;

            var channel = observation.Channel?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!AllowedChannels.Contains(channel)) return false;

            var role = string.IsNullOrWhiteSpace(observation.RecipientRole) ? null : observation.RecipientRole.Trim().ToUpperInvariant();
            if (role != null && !AllowedRoles.Contains(role)) return false;

            var kind = observation.RecipientKind?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!AllowedRecipientKinds.Contains(kind)) return false;

            var evidence = observation.RecipientEvidenceType?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!EvidenceByKind.TryGetValue(kind, out var allowedEvidence) || !allowedEvidence.Contains(evidence)) return false;

            var value = observation.RecipientValue?.Trim() ?? string.Empty;
            if (value.Length == 0 || value.Length > 320) return false;
            if (kind == "EMAIL")
            {
                value = value.ToLowerInvariant();
                if (!value.Contains('@') || value.Any(char.IsWhiteSpace)) return false;
            }

            var fileName = observation.FileName?.Trim() ?? string.Empty;
            if (fileName.Length == 0 || fileName.Length > 260) return false;
            if (observation.FileSizeBytes < 0) return false;

            var fileHash = observation.FileHashBytes?.Trim().ToLowerInvariant();
            if (fileHash != null && (fileHash.Length != 64 || !fileHash.All(Uri.IsHexDigit))) return false;

            var fingerprint = observation.ContentFingerprint?.Trim().ToLowerInvariant();
            if (fingerprint != null && (fingerprint.Length == 0 || fingerprint.Length > 64)) return false;

            // Reject timestamps far from the server clock. A device with a bad clock must not write events dated years away.
            if (observation.ObservedAtUtc < receivedAtUtc.AddDays(-7) || observation.ObservedAtUtc > receivedAtUtc.AddDays(1)) return false;

            normalized = new NormalizedObservation(
                observation.SendActionId,
                observation.RecipientOrdinal,
                channel,
                role,
                kind,
                value,
                evidence,
                fileName,
                observation.FileSizeBytes,
                fileHash,
                fingerprint,
                observation.ObservedAtUtc,
                observation.ExtensionVersion?.Trim());
            return true;
        }

        private sealed record NormalizedObservation(
            Guid SendActionId,
            int RecipientOrdinal,
            string Channel,
            string? RecipientRole,
            string RecipientKind,
            string RecipientValue,
            string RecipientEvidenceType,
            string FileName,
            long FileSizeBytes,
            string? FileHash,
            string? ContentFingerprint,
            DateTimeOffset ObservedAtUtc,
            string? ExtensionVersion);
    }
}
