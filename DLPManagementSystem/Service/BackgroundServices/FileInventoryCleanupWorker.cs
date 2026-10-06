using DLPManagementSystem.Helper.FileInventory;
using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Service.BackgroundServices
{
    // Hard-deletes FileInventoryRecords rows that have been soft-deleted (IsDeleted=true) for longer
    // than FileInventoryCleanup:RetentionDays, and removes captured file text (FileVersionContent) older than
    // the same window while keeping its version history. Simpler than AuditRetentionWorker - there is nothing to
    // archive here (a stale file-inventory row carries no investigative/audit value once the file is
    // gone and the row has already aged out; AuditEvents' archive-before-purge caution is about
    // evidence, this is just bookkeeping), so this is a straight batched ExecuteDeleteAsync loop.
    public class FileInventoryCleanupWorker : BackgroundService
    {
        private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<FileInventoryCleanupOptions> _options;
        private readonly ILogger<FileInventoryCleanupWorker> _logger;

        public FileInventoryCleanupWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<FileInventoryCleanupOptions> options,
            ILogger<FileInventoryCleanupWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunCleanupCycleAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Unexpected error during file inventory cleanup run.");
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

        private async Task RunCleanupCycleAsync(CancellationToken cancellationToken)
        {
            var options = _options.Value;

            if (!options.IsEnabled)
            {
                _logger.LogDebug("File inventory cleanup is disabled (FileInventoryCleanup:RetentionDays <= 0); skipping.");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DLPSystemContext>();

            var cutoffUtc = DateTimeOffset.UtcNow.AddDays(-options.RetentionDays);
            var totalDeleted = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchIds = await db.FileInventoryRecords
                    .Where(x => x.IsDeleted && x.DeletedAtUtc != null && x.DeletedAtUtc < cutoffUtc)
                    .OrderBy(x => x.DeletedAtUtc)
                    .Take(options.BatchSize)
                    .Select(x => x.FileId)
                    .ToListAsync(cancellationToken);

                if (batchIds.Count == 0)
                {
                    break;
                }

                await db.FileInventoryRecords
                    .Where(x => batchIds.Contains(x.FileId))
                    .ExecuteDeleteAsync(cancellationToken);

                totalDeleted += batchIds.Count;

                // A batch smaller than BatchSize means this was the last page - stop instead of looping
                // once more to confirm zero, same short-circuit AuditRetentionWorker's batching relies on.
                if (batchIds.Count < options.BatchSize)
                {
                    break;
                }
            }

            // Browser send observations (Phase 7) expire on the same retention window. They are append-only, so they
            // are removed in batches by ReceivedAtUtc, never edited.
            var expiredTransferCount = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchIds = await db.FileTransferEvents
                    .Where(x => x.ReceivedAtUtc < cutoffUtc)
                    .OrderBy(x => x.ReceivedAtUtc)
                    .Take(options.BatchSize)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (batchIds.Count == 0)
                {
                    break;
                }

                await db.FileTransferEvents
                    .Where(x => batchIds.Contains(x.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                expiredTransferCount += batchIds.Count;

                if (batchIds.Count < options.BatchSize)
                {
                    break;
                }
            }

            if (expiredTransferCount > 0)
            {
                _logger.LogInformation(
                    "File inventory cleanup run: removed {ExpiredTransferCount} file transfer events received before {CutoffUtc:o}.",
                    expiredTransferCount, cutoffUtc);
            }

            // Captured file text (File Tracking diff viewer) expires on the same retention window, but on its own track:
            // the text is the sensitive part, so it goes once it passes the window, while the version history it belongs
            // to is kept.
            var expiredTextCount = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchIds = await db.FileVersionContents
                    .Where(x => x.CapturedAtUtc < cutoffUtc)
                    .OrderBy(x => x.CapturedAtUtc)
                    .Take(options.BatchSize)
                    .Select(x => x.FileVersionHistoryId)
                    .ToListAsync(cancellationToken);

                if (batchIds.Count == 0)
                {
                    break;
                }

                await db.FileVersionContents
                    .Where(x => batchIds.Contains(x.FileVersionHistoryId))
                    .ExecuteDeleteAsync(cancellationToken);

                expiredTextCount += batchIds.Count;

                if (batchIds.Count < options.BatchSize)
                {
                    break;
                }
            }

            if (expiredTextCount > 0)
            {
                _logger.LogInformation(
                    "File inventory cleanup run: removed {ExpiredTextCount} captured file texts older than {CutoffUtc:o}.",
                    expiredTextCount, cutoffUtc);
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation(
                    "File inventory cleanup run: hard-deleted {DeletedCount} soft-deleted rows older than {CutoffUtc:o}.",
                    totalDeleted, cutoffUtc);
            }
            else
            {
                _logger.LogDebug("File inventory cleanup run found nothing eligible (cutoff {CutoffUtc:o}).", cutoffUtc);
            }
        }
    }
}
