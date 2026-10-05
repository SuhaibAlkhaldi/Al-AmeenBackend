using System.Text;
using System.Text.Json;
using DLPManagementSystem.Common;
using DLPManagementSystem.Helper.FileTracking;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Service.BackgroundServices
{
    // Phase 5 of the "File Tracking" plan: detects the same content (same fingerprint) held by several
    // distinct employees inside one sliding window, and raises one alert per continuous burst.
    //
    // Detection-and-alert only. The worker never modifies files, devices, or permissions. Every write is an
    // additive AuditEvent, Alert, or FileTrackingAlertEpisode row, so a re-run over the same data changes
    // nothing: a burst that was already recorded only has its last-observed time advanced.
    //
    // Each pass re-reads a horizon longer than the window (2 x window + poll interval), so a burst that
    // straddles two passes is seen whole in the second one and matches its existing episode instead of
    // raising a second alert.
    public sealed class RapidFilePropagationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RapidFilePropagationOptions> options,
        ILogger<RapidFilePropagationWorker> logger) : BackgroundService
    {
        private const string RuleType = "RapidPropagation";
        private const string EventTypeName = "RapidFilePropagation";

        private sealed record ObservationRow(
            Guid OrganizationId,
            string Fingerprint,
            DateTimeOffset ObservedAtUtc,
            Guid EmployeeId,
            Guid DeviceId,
            Guid FileId);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunDetectionPassAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Unexpected error during rapid file propagation detection.");
                }

                try
                {
                    var pollInterval = TimeSpan.FromMinutes(Math.Max(1, options.Value.PollIntervalMinutes));
                    await Task.Delay(pollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown.
                }
            }
        }

        private async Task RunDetectionPassAsync(CancellationToken cancellationToken)
        {
            var settings = options.Value;
            if (!settings.IsEnabled)
            {
                return;
            }

            var window = TimeSpan.FromMinutes(Math.Max(1, settings.WindowMinutes));
            var horizonUtc = DateTimeOffset.UtcNow - (window + window + TimeSpan.FromMinutes(Math.Max(1, settings.PollIntervalMinutes)));

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DLPSystemContext>();

            // Only real appearances of content count: a change that actually altered the content, reported by a
            // known employee, carrying a fingerprint. Copy events and classification-only changes do not add
            // a new appearance, and rows without an employee cannot be counted as a distinct person.
            var observations = await db.FileVersionHistories
                .AsNoTracking()
                .Where(x => x.ObservedAtUtc >= horizonUtc
                    && x.EventType == "Change"
                    && x.ContentChanged
                    && x.NewContentFingerprint != null
                    && x.EmployeeId != null)
                .Select(x => new ObservationRow(
                    x.OrganizationId,
                    x.NewContentFingerprint!,
                    x.ObservedAtUtc,
                    x.EmployeeId!.Value,
                    x.DeviceId,
                    x.FileId))
                .ToListAsync(cancellationToken);

            if (observations.Count < settings.EmployeeThreshold)
            {
                return;
            }

            var lookups = await ResolveLookupsAsync(db, cancellationToken);
            if (lookups is null)
            {
                logger.LogWarning("Rapid file propagation: required AuditEventType/AuditDecision/AlertStatus/AlertLevel rows are missing; skipping this pass.");
                return;
            }

            var groups = observations.GroupBy(o => (o.OrganizationId, o.Fingerprint));
            var changed = false;

            foreach (var group in groups)
            {
                var rows = group.OrderBy(o => o.ObservedAtUtc).ToList();
                var detected = RapidPropagationDetector.FindClusters(
                    rows.Select(r => new PropagationObservation(r.ObservedAtUtc, r.EmployeeId, r.DeviceId)).ToList(),
                    settings.EmployeeThreshold,
                    window);

                foreach (var cluster in detected)
                {
                    changed |= await RecordClusterAsync(db, group.Key.OrganizationId, group.Key.Fingerprint, rows, cluster, window, lookups, cancellationToken);
                }
            }

            if (changed)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        // Returns true when a new episode and alert were added, false when an existing episode was only extended.
        private static async Task<bool> RecordClusterAsync(
            DLPSystemContext db,
            Guid organizationId,
            string fingerprint,
            IReadOnlyList<ObservationRow> rows,
            PropagationCluster cluster,
            TimeSpan window,
            RapidLookups lookups,
            CancellationToken cancellationToken)
        {
            // A burst continues an open episode when that episode's last qualifying observation is within one
            // window of this burst's start. Otherwise the burst is new.
            var continuing = await db.FileTrackingAlertEpisodes
                .Where(e => e.OrganizationId == organizationId
                    && e.ContentFingerprint == fingerprint
                    && e.RuleType == RuleType
                    && e.EpisodeLastObservedAtUtc >= cluster.StartUtc - window)
                .OrderByDescending(e => e.EpisodeLastObservedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (continuing is not null)
            {
                if (cluster.EndUtc > continuing.EpisodeLastObservedAtUtc)
                {
                    continuing.EpisodeLastObservedAtUtc = cluster.EndUtc;
                    continuing.DistinctEmployeeCount = Math.Max(continuing.DistinctEmployeeCount, cluster.DistinctEmployeeCount);
                    continuing.DistinctDeviceCount = Math.Max(continuing.DistinctDeviceCount, cluster.DistinctDeviceCount);
                }
                return false;
            }

            var members = rows
                .Where(r => r.ObservedAtUtc >= cluster.StartUtc && r.ObservedAtUtc <= cluster.EndUtc)
                .OrderBy(r => r.ObservedAtUtc)
                .ToList();
            var latest = members[^1];
            var sampleFileId = members[0].FileId;

            var employeeIds = members.Select(m => m.EmployeeId).Distinct().ToList();
            var deviceIds = members.Select(m => m.DeviceId).Distinct().ToList();
            var employeeNames = await db.Employees.AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.DisplayName, cancellationToken);
            var deviceNames = await db.Devices.AsNoTracking()
                .Where(d => deviceIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.MachineName, cancellationToken);
            var file = await db.FileInventoryRecords.AsNoTracking()
                .Where(f => f.FileId == sampleFileId)
                .Select(f => new { f.FileName, f.FilePath })
                .FirstOrDefaultAsync(cancellationToken);

            var fileName = file?.FileName ?? "(unknown file)";
            var filePath = file?.FilePath ?? string.Empty;
            var nowUtc = DateTimeOffset.UtcNow;

            var metadata = JsonSerializer.Serialize(new
            {
                contentFingerprint = fingerprint,
                distinctEmployeeCount = cluster.DistinctEmployeeCount,
                distinctDeviceCount = cluster.DistinctDeviceCount,
                windowMinutes = (int)window.TotalMinutes,
                episodeStartUtc = cluster.StartUtc,
                episodeEndUtc = cluster.EndUtc,
                observations = members.Select(m => new
                {
                    observedAtUtc = m.ObservedAtUtc,
                    employeeId = m.EmployeeId,
                    employeeName = employeeNames.GetValueOrDefault(m.EmployeeId),
                    deviceId = m.DeviceId,
                    deviceName = deviceNames.GetValueOrDefault(m.DeviceId)
                })
            });

            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DeviceId = latest.DeviceId,
                EmployeeId = latest.EmployeeId,
                ActionKey = PermissionActionKeys.FileInventoryMonitor,
                EventTypeId = lookups.EventTypeId,
                DecisionId = lookups.DecisionId,
                OccurredAtUtc = cluster.EndUtc,
                ReceivedAtUtc = nowUtc,
                MetadataJson = metadata
            };
            db.AuditEvents.Add(auditEvent);

            db.FileTrackingAlertEpisodes.Add(new FileTrackingAlertEpisode
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ContentFingerprint = fingerprint,
                RuleType = RuleType,
                EpisodeAnchorAtUtc = cluster.StartUtc,
                EpisodeLastObservedAtUtc = cluster.EndUtc,
                DistinctEmployeeCount = cluster.DistinctEmployeeCount,
                DistinctDeviceCount = cluster.DistinctDeviceCount,
                AuditEventId = auditEvent.Id,
                CreatedAtUtc = nowUtc
            });

            db.Alerts.Add(new Alert
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AuditEventId = auditEvent.Id,
                AlertLevelId = lookups.AlertLevelId,
                AlertStatusId = lookups.AlertStatusId,
                CreatedAtUtc = nowUtc,
                Title = BuildTitle(fileName, members, employeeNames, cluster.DistinctEmployeeCount),
                Description = BuildDescription(fileName, filePath, fingerprint, members, employeeNames, deviceNames, cluster, window)
            });

            return true;
        }

        // States what was observed and nothing more: no claim that anyone sent the file to anyone.
        // Names everyone involved in the title itself, so the alerts list shows who the content reached
        // without opening the alert. Capped to the column length.
        private static string BuildTitle(
            string fileName,
            IReadOnlyList<ObservationRow> members,
            IReadOnlyDictionary<Guid, string> employeeNames,
            int distinctEmployeeCount)
        {
            var names = members
                .OrderBy(m => m.ObservedAtUtc)
                .Select(m => m.EmployeeId)
                .Distinct()
                .Select(id => employeeNames.GetValueOrDefault(id, "(unknown employee)"));

            var title = $"Rapid file propagation — {fileName} — {distinctEmployeeCount} employees: {string.Join(", ", names)}";
            return title.Length <= 200 ? title : title[..200];
        }

        private static string BuildDescription(
            string fileName,
            string filePath,
            string fingerprint,
            IReadOnlyList<ObservationRow> members,
            IReadOnlyDictionary<Guid, string> employeeNames,
            IReadOnlyDictionary<Guid, string> deviceNames,
            PropagationCluster cluster,
            TimeSpan window)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"File: {fileName} ({filePath})");
            builder.AppendLine($"Content fingerprint: {fingerprint[..16]}…");
            builder.AppendLine("Observed on:");
            var firstSightings = members
                .GroupBy(m => (m.EmployeeId, m.DeviceId))
                .Select(g => g.First())
                .OrderBy(m => m.ObservedAtUtc);
            foreach (var member in firstSightings)
            {
                var employee = employeeNames.GetValueOrDefault(member.EmployeeId, "(unknown employee)");
                var device = deviceNames.GetValueOrDefault(member.DeviceId, "(unknown device)");
                builder.AppendLine($"  {employee} / {device} / {member.ObservedAtUtc:yyyy-MM-dd HH:mm} UTC");
            }
            builder.AppendLine($"Distinct employees: {cluster.DistinctEmployeeCount} | Distinct devices: {cluster.DistinctDeviceCount}");
            builder.Append($"Window: {(int)window.TotalMinutes} minutes | Detection method: same content fingerprint");
            return builder.ToString();
        }

        private static async Task<RapidLookups?> ResolveLookupsAsync(DLPSystemContext db, CancellationToken cancellationToken)
        {
            var eventTypeId = await db.AuditEventTypes.AsNoTracking()
                .Where(x => x.Name == EventTypeName)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var decisionId = await db.AuditDecisions.AsNoTracking()
                .Where(x => x.Name == "Error")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var alertStatusId = await db.AlertStatuses.AsNoTracking()
                .Where(x => x.Name == "New")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var alertLevelId = await db.AlertLevels.AsNoTracking()
                .Where(x => x.Name == "High")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!eventTypeId.HasValue || !decisionId.HasValue || !alertStatusId.HasValue || !alertLevelId.HasValue)
            {
                return null;
            }

            return new RapidLookups(eventTypeId.Value, decisionId.Value, alertStatusId.Value, alertLevelId.Value);
        }

        private sealed record RapidLookups(int EventTypeId, int DecisionId, int AlertStatusId, int AlertLevelId);
    }
}
