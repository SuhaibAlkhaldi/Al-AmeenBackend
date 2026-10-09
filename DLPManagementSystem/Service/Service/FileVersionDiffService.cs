using System.Security.Cryptography;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileTracking;
using DLPManagementSystem.Helper.FileTracking;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    // Builds the line-by-line diff between one recorded file version and the version before it (File Tracking Phase 6).
    // Texts are decrypted only inside this call, compared, and returned. Nothing is cached, and nothing from the text
    // is logged: the audit line records who looked, at which version, and the outcome, never the content.
    public class FileVersionDiffService : IFileVersionDiffService
    {
        // Unchanged lines shown on each side of a change. Anything further away is folded into one "gap" entry.
        private const int ContextLines = 3;

        private const string EventTypeChange = "Change";
        private const string ReasonTextNotAvailable = "text_not_available";
        private const string ReasonPreviousTextNotAvailable = "previous_text_not_available";
        private const string ReasonTooManyChanges = "too_many_changes";
        private const string ReasonNotAContentChange = "not_a_content_change";

        private readonly DLPSystemContext _db;
        private readonly IFileVersionTextProtector _textProtector;
        private readonly ILogger<FileVersionDiffService> _logger;

        public FileVersionDiffService(DLPSystemContext db, IFileVersionTextProtector textProtector, ILogger<FileVersionDiffService> logger)
        {
            _db = db;
            _textProtector = textProtector;
            _logger = logger;
        }

        public async Task<ApiResponse<FileVersionDiffDto>> GetDiffAsync(Guid organizationId, Guid userId, Guid versionId, CancellationToken cancellationToken)
        {
            var target = await _db.FileVersionHistories
                .AsNoTracking()
                .Where(x => x.Id == versionId && x.OrganizationId == organizationId)
                .Select(x => new
                {
                    x.Id,
                    x.FileId,
                    x.ObservedAtUtc,
                    x.EventType,
                    x.ContentChanged,
                    x.NewClassificationTier,
                    DeviceName = x.Device.MachineName,
                    EmployeeName = x.Employee != null ? x.Employee.DisplayName : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (target is null)
            {
                return ApiResponse<FileVersionDiffDto>.FailureResponse(
                    "File version was not found.",
                    "لم يتم العثور على نسخة الملف");
            }

            var record = await _db.FileInventoryRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FileId == target.FileId && x.OrganizationId == organizationId, cancellationToken);

            // The same lineage the File History timeline uses, so "previous version" means the same thing in both views.
            var lineageFileIds = record is null
                ? new List<Guid> { target.FileId }
                : (await FileLineage.ResolveAsync(_db, organizationId, record, cancellationToken)).Select(x => x.FileId).ToList();

            var dto = new FileVersionDiffDto
            {
                VersionId = target.Id,
                FileId = target.FileId,
                FileName = record?.FileName ?? string.Empty,
                DeviceName = target.DeviceName,
                EmployeeName = target.EmployeeName,
                ObservedAtUtc = target.ObservedAtUtc,
                ClassificationTier = target.NewClassificationTier
            };

            await FillDiffAsync(dto, organizationId, lineageFileIds, target.EventType, target.ContentChanged, target.ObservedAtUtc, cancellationToken);

            _logger.LogInformation(
                "File version diff viewed. OrganizationId={OrganizationId} UserId={UserId} FileId={FileId} VersionId={VersionId} PreviousVersionId={PreviousVersionId} Available={Available} Reason={Reason} Added={Added} Removed={Removed}",
                organizationId, userId, dto.FileId, dto.VersionId, dto.PreviousVersionId, dto.IsAvailable, dto.UnavailableReason, dto.AddedLineCount, dto.RemovedLineCount);

            return ApiResponse<FileVersionDiffDto>.SuccessResponse(dto);
        }

        private async Task FillDiffAsync(
            FileVersionDiffDto dto,
            Guid organizationId,
            List<Guid> lineageFileIds,
            string eventType,
            bool contentChanged,
            DateTimeOffset observedAtUtc,
            CancellationToken cancellationToken)
        {
            // A full-copy or classification-only row has no content transition to show.
            if (eventType != EventTypeChange || !contentChanged)
            {
                dto.UnavailableReason = ReasonNotAContentChange;
                return;
            }

            var newText = await LoadTextAsync(dto.VersionId, cancellationToken);
            if (newText is null)
            {
                dto.UnavailableReason = ReasonTextNotAvailable;
                return;
            }

            // The version before this one is the latest content change of the same file lineage (see FileLineage), so a
            // rename or re-tag does not make a file look new. Full-copy and classification rows are skipped on purpose:
            // they do not change the text the reviewer is comparing.
            var previous = await _db.FileVersionHistories
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && lineageFileIds.Contains(x.FileId)
                    && x.EventType == EventTypeChange && x.ContentChanged && x.ObservedAtUtc < observedAtUtc)
                .OrderByDescending(x => x.ObservedAtUtc)
                .Select(x => new { x.Id })
                .FirstOrDefaultAsync(cancellationToken);

            string oldText;
            if (previous is null)
            {
                dto.IsFirstVersion = true;
                oldText = string.Empty;
            }
            else
            {
                dto.PreviousVersionId = previous.Id;
                var loadedPrevious = await LoadTextAsync(previous.Id, cancellationToken);
                if (loadedPrevious is null)
                {
                    dto.UnavailableReason = ReasonPreviousTextNotAvailable;
                    return;
                }
                oldText = loadedPrevious;
            }

            var changes = LineDiff.Compare(SplitLines(oldText), SplitLines(newText));
            if (changes is null)
            {
                dto.UnavailableReason = ReasonTooManyChanges;
                return;
            }

            dto.IsAvailable = true;
            dto.AddedLineCount = changes.Count(change => change.Kind == LineChangeKind.Added);
            dto.RemovedLineCount = changes.Count(change => change.Kind == LineChangeKind.Removed);
            dto.Lines = FoldUnchanged(changes);
        }

        // Returns null when the text is not stored (public tier, too large, or past retention) or cannot be decrypted.
        // A decryption failure is logged without any content, because the key ring changed or the row is damaged, and
        // either way the reviewer should see "not available", not an error page.
        private async Task<string?> LoadTextAsync(Guid versionId, CancellationToken cancellationToken)
        {
            var encrypted = await _db.FileVersionContents
                .AsNoTracking()
                .Where(x => x.FileVersionHistoryId == versionId)
                .Select(x => x.EncryptedText)
                .FirstOrDefaultAsync(cancellationToken);

            if (encrypted is null) return null;

            try
            {
                return _textProtector.Unprotect(encrypted);
            }
            catch (CryptographicException exception)
            {
                _logger.LogError(exception, "Could not decrypt the stored text of file version {VersionId}.", versionId);
                return null;
            }
        }

        private static IReadOnlyList<string> SplitLines(string text) =>
            text.Length == 0 ? Array.Empty<string>() : text.Split('\n');

        // Keeps every changed line plus ContextLines unchanged lines either side of it, and collapses each longer
        // unchanged run into one gap entry. Line numbers advance through the folded lines too, so the numbers stay true.
        private static List<FileVersionDiffLineDto> FoldUnchanged(IReadOnlyList<LineChange> changes)
        {
            var keep = new bool[changes.Count];
            for (var index = 0; index < changes.Count; index++)
            {
                if (changes[index].Kind == LineChangeKind.Equal) continue;

                var from = Math.Max(0, index - ContextLines);
                var to = Math.Min(changes.Count - 1, index + ContextLines);
                for (var near = from; near <= to; near++)
                {
                    keep[near] = true;
                }
            }

            var lines = new List<FileVersionDiffLineDto>();
            var oldLineNumber = 0;
            var newLineNumber = 0;
            var skipped = 0;

            for (var index = 0; index < changes.Count; index++)
            {
                var change = changes[index];
                var oldNumber = change.Kind != LineChangeKind.Added ? ++oldLineNumber : (int?)null;
                var newNumber = change.Kind != LineChangeKind.Removed ? ++newLineNumber : (int?)null;

                if (!keep[index])
                {
                    skipped++;
                    continue;
                }

                if (skipped > 0)
                {
                    lines.Add(new FileVersionDiffLineDto { Kind = "gap", SkippedLineCount = skipped });
                    skipped = 0;
                }

                lines.Add(new FileVersionDiffLineDto
                {
                    Kind = change.Kind switch
                    {
                        LineChangeKind.Added => "added",
                        LineChangeKind.Removed => "removed",
                        _ => "equal"
                    },
                    Text = change.Text,
                    OldLineNumber = oldNumber,
                    NewLineNumber = newNumber
                });
            }

            if (skipped > 0)
            {
                lines.Add(new FileVersionDiffLineDto { Kind = "gap", SkippedLineCount = skipped });
            }

            return lines;
        }
    }
}
