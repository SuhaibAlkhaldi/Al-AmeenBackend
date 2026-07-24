using System.IO.Compression;
using System.Text.Json;
using DLPManagementSystem.Common;
using DLPManagementSystem.Helper.Retention;
using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Service.BackgroundServices
{
    // Archives (gzip-compressed CSV, one file per organization per run) then purges AuditEvents older than
    // AuditRetention:RetentionDays, together with their Closed Alert / AiAnalysisResult / AiAnalysisOverride
    // dependents (all three have required, non-nullable FKs back to AuditEvents, so they must be archived and
    // removed in the same operation — never left orphaned). An AuditEvent with any non-Closed Alert is never
    // purged, regardless of age: an open investigation's evidence must not disappear.
    public class AuditRetentionWorker : BackgroundService
    {
        private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostEnvironment _environment;
        private readonly IOptions<AuditRetentionOptions> _options;
        private readonly ILogger<AuditRetentionWorker> _logger;

        public AuditRetentionWorker(
            IServiceScopeFactory scopeFactory,
            IHostEnvironment environment,
            IOptions<AuditRetentionOptions> options,
            ILogger<AuditRetentionWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunRetentionCycleAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Unexpected error during audit retention run.");
                }

                try
                {
                    await Task.Delay(RunInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown.
                }
            }
        }

        private async Task RunRetentionCycleAsync(CancellationToken cancellationToken)
        {
            var options = _options.Value;

            if (!options.IsEnabled)
            {
                _logger.LogDebug("Audit retention is disabled (AuditRetention:RetentionDays not set); skipping.");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DLPSystemContext>();

            var closedStatusId = await db.AlertStatuses
                .Where(s => s.Name == "Closed")
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (closedStatusId == null)
            {
                _logger.LogWarning("Audit retention: 'Closed' AlertStatus lookup value was not found; skipping this run.");
                return;
            }

            var cutoffUtc = DateTimeOffset.UtcNow.AddDays(-options.RetentionDays);

            var candidates = await db.AuditEvents
                .Where(x => x.OccurredAtUtc < cutoffUtc)
                .Where(x => !db.Alerts.Any(a => a.AuditEventId == x.Id && a.AlertStatusId != closedStatusId.Value))
                .OrderBy(x => x.OccurredAtUtc)
                .Select(x => new { x.Id, x.OrganizationId })
                .ToListAsync(cancellationToken);

            var skippedCountByOrg = (await db.AuditEvents
                .Where(x => x.OccurredAtUtc < cutoffUtc)
                .Where(x => db.Alerts.Any(a => a.AuditEventId == x.Id && a.AlertStatusId != closedStatusId.Value))
                .GroupBy(x => x.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.OrganizationId, x => x.Count);

            if (candidates.Count == 0)
            {
                _logger.LogInformation("Audit retention run found nothing eligible to archive (cutoff {CutoffUtc:o}).", cutoffUtc);
                return;
            }

            var archiveDirectory = Path.Combine(_environment.ContentRootPath, options.ArchiveDirectory);
            Directory.CreateDirectory(archiveDirectory);

            var runTimestampUtc = DateTimeOffset.UtcNow;

            foreach (var orgGroup in candidates.GroupBy(x => x.OrganizationId))
            {
                await ArchiveAndPurgeOrganizationAsync(
                    db, orgGroup.Key, orgGroup.Select(x => x.Id).ToList(), closedStatusId.Value,
                    archiveDirectory, runTimestampUtc, options.BatchSize,
                    skippedCountByOrg.GetValueOrDefault(orgGroup.Key), cancellationToken);
            }
        }

        private async Task ArchiveAndPurgeOrganizationAsync(
            DLPSystemContext db,
            Guid organizationId,
            List<Guid> auditEventIds,
            int closedStatusId,
            string archiveDirectory,
            DateTimeOffset runTimestampUtc,
            int batchSize,
            int skippedCount,
            CancellationToken cancellationToken)
        {
            var fileName = $"audit-events-archive-{organizationId}-{runTimestampUtc:yyyyMMddHHmmss}.csv.gz";
            var filePath = Path.Combine(archiveDirectory, fileName);

            var archivedEvents = 0;
            var archivedAlerts = 0;
            var archivedAiResults = 0;
            var archivedOverrides = 0;

            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            await using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal, leaveOpen: true))
            await using (var writer = new StreamWriter(gzipStream))
            {
                writer.NewLine = "\r\n";
                await writer.WriteLineAsync(CsvWriterHelper.BuildRow("RecordType", "AuditEventId", "RecordId", "DataJson"));

                for (var offset = 0; offset < auditEventIds.Count; offset += batchSize)
                {
                    var batchIds = auditEventIds.Skip(offset).Take(batchSize).ToList();

                    var auditEvents = await db.AuditEvents.AsNoTracking()
                        .Where(x => batchIds.Contains(x.Id))
                        .ToListAsync(cancellationToken);

                    var alerts = await db.Alerts.AsNoTracking()
                        .Where(a => batchIds.Contains(a.AuditEventId) && a.AlertStatusId == closedStatusId)
                        .ToListAsync(cancellationToken);

                    var aiResults = await db.AiAnalysisResults.AsNoTracking()
                        .Where(r => batchIds.Contains(r.AuditEventId))
                        .ToListAsync(cancellationToken);

                    var aiResultIds = aiResults.Select(r => r.Id).ToList();
                    var aiOverrides = aiResultIds.Count == 0
                        ? new List<AiAnalysisOverride>()
                        : await db.AiAnalysisOverrides.AsNoTracking()
                            .Where(o => aiResultIds.Contains(o.AiAnalysisResultId))
                            .ToListAsync(cancellationToken);

                    var aiResultToAuditEvent = aiResults.ToDictionary(r => r.Id, r => r.AuditEventId);

                    foreach (var auditEvent in auditEvents)
                    {
                        await writer.WriteLineAsync(CsvWriterHelper.BuildRow(
                            "AuditEvent", auditEvent.Id.ToString(), auditEvent.Id.ToString(), SerializeAuditEvent(auditEvent)));
                    }

                    foreach (var alert in alerts)
                    {
                        await writer.WriteLineAsync(CsvWriterHelper.BuildRow(
                            "Alert", alert.AuditEventId.ToString(), alert.Id.ToString(), SerializeAlert(alert)));
                    }

                    foreach (var aiResult in aiResults)
                    {
                        await writer.WriteLineAsync(CsvWriterHelper.BuildRow(
                            "AiAnalysisResult", aiResult.AuditEventId.ToString(), aiResult.Id.ToString(), SerializeAiResult(aiResult)));
                    }

                    foreach (var aiOverride in aiOverrides)
                    {
                        var ownerAuditEventId = aiResultToAuditEvent.GetValueOrDefault(aiOverride.AiAnalysisResultId);
                        await writer.WriteLineAsync(CsvWriterHelper.BuildRow(
                            "AiAnalysisOverride", ownerAuditEventId.ToString(), aiOverride.Id.ToString(), SerializeAiOverride(aiOverride)));
                    }

                    // Flush every layer so the archived bytes are actually out of process memory and on disk
                    // before any deletion for this batch begins — never delete data that isn't safely archived.
                    await writer.FlushAsync(cancellationToken);
                    await gzipStream.FlushAsync(cancellationToken);
                    await fileStream.FlushAsync(cancellationToken);

                    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        if (aiOverrides.Count > 0)
                        {
                            var overrideIds = aiOverrides.Select(o => o.Id).ToList();
                            await db.AiAnalysisOverrides.Where(o => overrideIds.Contains(o.Id)).ExecuteDeleteAsync(cancellationToken);
                        }

                        if (aiResults.Count > 0)
                        {
                            await db.AiAnalysisResults.Where(r => batchIds.Contains(r.AuditEventId)).ExecuteDeleteAsync(cancellationToken);
                        }

                        if (alerts.Count > 0)
                        {
                            await db.Alerts.Where(a => batchIds.Contains(a.AuditEventId) && a.AlertStatusId == closedStatusId)
                                .ExecuteDeleteAsync(cancellationToken);
                        }

                        await db.AuditEvents.Where(x => batchIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);

                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }

                    archivedEvents += auditEvents.Count;
                    archivedAlerts += alerts.Count;
                    archivedAiResults += aiResults.Count;
                    archivedOverrides += aiOverrides.Count;
                }
            }

            _logger.LogInformation(
                "Audit retention run for organization {OrganizationId}: archived {ArchivedEvents} audit events " +
                "({ArchivedAlerts} closed alerts, {ArchivedAiResults} AI results, {ArchivedOverrides} AI overrides), " +
                "purged {PurgedEvents} audit events, skipped {SkippedEvents} due to open alerts. Archive: {ArchivePath}",
                organizationId, archivedEvents, archivedAlerts, archivedAiResults, archivedOverrides, archivedEvents, skippedCount, filePath);
        }

        private static string SerializeAuditEvent(AuditEvent x) => JsonSerializer.Serialize(new
        {
            x.Id, x.OrganizationId, x.BatchRowId, x.DeviceId, x.EmployeeId, x.UserSid, x.Username, x.ActionKey,
            x.EventTypeId, x.DecisionId, x.ReasonCodeId, x.PermissionGrantId, x.ObservedFileId, x.PolicyVersion,
            x.OccurredAtUtc, x.ReceivedAtUtc, x.AgentVersion, x.CorrelationId, x.MetadataJson
        }, JsonOptions);

        private static string SerializeAlert(Alert x) => JsonSerializer.Serialize(new
        {
            x.Id, x.OrganizationId, x.AuditEventId, x.AlertLevelId, x.AlertStatusId, x.Title, x.Description,
            x.AssignedToUserId, x.CreatedAtUtc, x.ClosedAtUtc, x.InvestigationNotes, x.IsFalsePositive, x.EmailNotifiedAtUtc
        }, JsonOptions);

        private static string SerializeAiResult(AiAnalysisResult x) => JsonSerializer.Serialize(new
        {
            x.Id, x.OrganizationId, x.AuditEventId, x.DecisionId, x.RiskScore, x.Reason, x.EngineName,
            x.EvaluationVersion, x.ConfidenceScore, x.ProcessingTimeMs, x.CreatedAtUtc
        }, JsonOptions);

        private static string SerializeAiOverride(AiAnalysisOverride x) => JsonSerializer.Serialize(new
        {
            x.Id, x.AiAnalysisResultId, x.AdminUserId, x.DecisionId, x.Reason, x.ExpiresAtUtc, x.IsTemporary, x.CreatedAtUtc
        }, JsonOptions);
    }
}
