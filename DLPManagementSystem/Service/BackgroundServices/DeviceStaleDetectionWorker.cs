using System.Text.Json;
using DLPManagementSystem.Common;
using DLPManagementSystem.Helper.DeviceMonitoring;
using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Service.BackgroundServices
{
    // Detects Active devices that have gone quiet (no heartbeat within DeviceStaleDetection:
    // StaleThresholdMinutes) and, critically, distinguishes "just offline" from "offline AND has not
    // confirmed applying the organization's latest published policy version" - the second case is the
    // same class of risk that caused the AppIDSvc/AppLocker incident this worker exists to catch
    // earlier: a policy change went out, the device stopped confirming it applied cleanly, and nothing
    // noticed automatically.
    //
    // Detection-and-alert only, deliberately, for this phase: automatically reverting a specific
    // device's effective PermissionGrants/PolicyVersion to a prior "known-good" state requires first
    // deciding what "known-good" even means here (a new explicit per-device field? the immediately
    // prior global PolicyVersion, even though PolicyVersion is organization-wide and not a per-device
    // snapshot? do PermissionGrant revocations need to be un-revoked, and is that ever safe to automate
    // without human review?) - none of that is answerable from the current schema, so it is left as an
    // explicit follow-up question rather than guessed at here. Every non-idempotency behavior below
    // stays purely additive (new AuditEvent/Alert rows only) - no PolicyVersion/PermissionGrant/Device
    // row is ever modified by this worker.
    public sealed class DeviceStaleDetectionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<DeviceStaleDetectionOptions> options,
        ILogger<DeviceStaleDetectionWorker> logger) : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunDetectionCycleAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Unexpected error during device stale-detection run.");
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

        private async Task RunDetectionCycleAsync(CancellationToken cancellationToken)
        {
            var settings = options.Value;
            if (!settings.IsEnabled)
            {
                logger.LogDebug("Device stale detection is disabled (DeviceStaleDetection:StaleThresholdMinutes not set); skipping.");
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DLPSystemContext>();

            var nowUtc = DateTimeOffset.UtcNow;
            var staleCutoffUtc = nowUtc.AddMinutes(-settings.StaleThresholdMinutes);

            var staleDevices = await db.Devices
                .AsNoTracking()
                .Where(d => d.Status.Name == "Active"
                    && d.LastSeenAtUtc != null
                    && d.LastSeenAtUtc < staleCutoffUtc)
                .Select(d => new
                {
                    d.Id,
                    d.OrganizationId,
                    d.MachineName,
                    LastSeenAtUtc = d.LastSeenAtUtc!.Value,
                    d.CurrentPolicyVersion
                })
                .ToListAsync(cancellationToken);

            if (staleDevices.Count == 0)
            {
                return;
            }

            var deviceIds = staleDevices.Select(d => d.Id).ToList();
            var organizationIds = staleDevices.Select(d => d.OrganizationId).Distinct().ToList();

            var policyStateByDevice = await db.DevicePolicyStates
                .AsNoTracking()
                .Where(x => deviceIds.Contains(x.DeviceId))
                .ToDictionaryAsync(x => x.DeviceId, cancellationToken);

            var latestVersionByOrganization = await db.PolicyVersions
                .AsNoTracking()
                .Where(v => organizationIds.Contains(v.OrganizationId))
                .GroupBy(v => v.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, LatestVersion = g.Max(v => v.VersionNumber) })
                .ToDictionaryAsync(x => x.OrganizationId, x => x.LatestVersion, cancellationToken);

            var eventTypeMap = await db.AuditEventTypes
                .AsNoTracking()
                .Where(x => x.Name == "DeviceStale" || x.Name == "DeviceStaleAfterPolicyChange")
                .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

            var reasonCodeMap = await db.AuditReasonCodes
                .AsNoTracking()
                .Where(x => x.Code == "DeviceOffline" || x.Code == "DeviceOfflineWithUnconfirmedPolicy")
                .ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);

            var errorDecisionId = await db.AuditDecisions
                .AsNoTracking()
                .Where(x => x.Name == "Error")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var newAlertStatusId = await db.AlertStatuses
                .AsNoTracking()
                .Where(x => x.Name == "New")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var highAlertLevelId = await db.AlertLevels
                .AsNoTracking()
                .Where(x => x.Name == "High")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!eventTypeMap.ContainsKey("DeviceStale") || !eventTypeMap.ContainsKey("DeviceStaleAfterPolicyChange")
                || !errorDecisionId.HasValue)
            {
                logger.LogWarning("Device stale detection: required AuditEventType/AuditDecision lookup rows are missing; skipping this run.");
                return;
            }

            var canCreateAlerts = newAlertStatusId.HasValue && highAlertLevelId.HasValue;
            var flaggedCount = 0;

            foreach (var device in staleDevices)
            {
                policyStateByDevice.TryGetValue(device.Id, out var policyState);
                var latestOrgVersion = latestVersionByOrganization.GetValueOrDefault(device.OrganizationId, 0);
                var lastAppliedVersion = policyState?.LastAppliedPolicyVersion ?? device.CurrentPolicyVersion;

                // Critical: the org has a published policy version this device has not confirmed
                // applying. Covers both "fetched it but never confirmed applying it" (LastFetchedPolicyVersion
                // > LastAppliedPolicyVersion) and "never even reconnected since" (LastAppliedPolicyVersion
                // behind the organization's current latest) - the second implies the first whenever it's
                // true, so one comparison against the organization's latest version covers both.
                var isCritical = latestOrgVersion > lastAppliedVersion;

                // Idempotency: skip if this exact staleness episode was already flagged. LastSeenAtUtc only
                // advances again once the device sends a fresh heartbeat, so an existing flag with
                // OccurredAtUtc at or after the device's current LastSeenAtUtc means "already reported for
                // this episode" - once the device reconnects and LastSeenAtUtc moves forward, a later
                // staleness episode is free to raise a new flag.
                var alreadyFlagged = await db.AuditEvents
                    .Where(x => x.DeviceId == device.Id
                        && x.OccurredAtUtc >= device.LastSeenAtUtc
                        && (x.EventTypeId == eventTypeMap["DeviceStale"] || x.EventTypeId == eventTypeMap["DeviceStaleAfterPolicyChange"]))
                    .AnyAsync(cancellationToken);

                if (alreadyFlagged)
                {
                    continue;
                }

                var eventTypeId = isCritical ? eventTypeMap["DeviceStaleAfterPolicyChange"] : eventTypeMap["DeviceStale"];
                var reasonCodeId = isCritical
                    ? reasonCodeMap.GetValueOrDefault("DeviceOfflineWithUnconfirmedPolicy")
                    : reasonCodeMap.GetValueOrDefault("DeviceOffline");

                var staleForMinutes = (int)(nowUtc - device.LastSeenAtUtc).TotalMinutes;

                var metadata = JsonSerializer.Serialize(new
                {
                    lastSeenAtUtc = device.LastSeenAtUtc,
                    staleForMinutes,
                    staleThresholdMinutes = settings.StaleThresholdMinutes,
                    lastFetchedPolicyVersion = policyState?.LastFetchedPolicyVersion,
                    lastAppliedPolicyVersion = lastAppliedVersion,
                    latestOrganizationPolicyVersion = latestOrgVersion
                }, JsonOptions);

                var auditEvent = new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = device.OrganizationId,
                    DeviceId = device.Id,
                    ActionKey = PermissionActionKeys.AgentSession,
                    EventTypeId = eventTypeId,
                    DecisionId = errorDecisionId.Value,
                    ReasonCodeId = reasonCodeId == 0 ? (int?)null : reasonCodeId,
                    PolicyVersion = lastAppliedVersion,
                    OccurredAtUtc = nowUtc,
                    ReceivedAtUtc = nowUtc,
                    MetadataJson = metadata
                };

                db.AuditEvents.Add(auditEvent);
                flaggedCount++;

                if (isCritical && canCreateAlerts)
                {
                    db.Alerts.Add(new Alert
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = device.OrganizationId,
                        AuditEventId = auditEvent.Id,
                        AlertLevelId = highAlertLevelId!.Value,
                        AlertStatusId = newAlertStatusId!.Value,
                        Title = $"Device stale after policy change: {device.MachineName}",
                        Description =
                            $"Device '{device.MachineName}' has not sent a heartbeat in {staleForMinutes} minutes " +
                            $"and has not confirmed applying the organization's latest policy version " +
                            $"({lastAppliedVersion} applied vs {latestOrgVersion} published). " +
                            "This device may still be enforcing a stale or partially-applied policy.",
                        CreatedAtUtc = nowUtc
                    });
                }
            }

            if (flaggedCount > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Device stale detection run flagged {FlaggedCount} device(s) out of {StaleCount} stale.", flaggedCount, staleDevices.Count);
            }
        }
    }
}
