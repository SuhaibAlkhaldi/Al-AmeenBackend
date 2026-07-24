using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.BackgroundServices
{
    // Polls for High/Critical alerts that haven't been emailed yet and notifies the roles that own alert
    // response (SuperAdmin/SecurityAdmin, per AlertsController's [Authorize] split on the
    // assign/status-update endpoints), scoped to each alert's own organization.
    public class AlertEmailNotificationWorker : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);
        private static readonly string[] NotifiableLevels = { "High", "Critical" };
        private static readonly string[] RecipientRoles = { "SuperAdmin", "SecurityAdmin" };

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AlertEmailNotificationWorker> _logger;

        public AlertEmailNotificationWorker(IServiceScopeFactory scopeFactory, ILogger<AlertEmailNotificationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingAlertsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Unexpected error while polling for alert email notifications.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown.
                }
            }
        }

        private async Task ProcessPendingAlertsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DLPSystemContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IAlertEmailService>();

            var pendingAlerts = await db.Alerts
                .Include(x => x.AlertLevel)
                .Include(x => x.AuditEvent).ThenInclude(x => x.Device)
                .Include(x => x.AuditEvent).ThenInclude(x => x.Employee)
                .Where(x => x.EmailNotifiedAtUtc == null && NotifiableLevels.Contains(x.AlertLevel.Name))
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (pendingAlerts.Count == 0)
            {
                return;
            }

            var recipientCacheByOrganization = new Dictionary<Guid, List<User>>();

            foreach (var alert in pendingAlerts)
            {
                try
                {
                    if (!recipientCacheByOrganization.TryGetValue(alert.OrganizationId, out var recipients))
                    {
                        recipients = await db.Users
                            .Where(u => u.OrganizationId == alert.OrganizationId
                                && u.Status.Name == "Active"
                                && RecipientRoles.Contains(u.Role.Name))
                            .ToListAsync(cancellationToken);

                        recipientCacheByOrganization[alert.OrganizationId] = recipients;
                    }

                    if (recipients.Count == 0)
                    {
                        _logger.LogWarning(
                            "Alert {AlertId} has no Active SuperAdmin/SecurityAdmin recipients in organization {OrganizationId}; will retry next cycle.",
                            alert.Id, alert.OrganizationId);
                        continue;
                    }

                    await emailService.SendAlertNotificationAsync(alert, recipients, cancellationToken);

                    alert.EmailNotifiedAtUtc = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Leave EmailNotifiedAtUtc null so the next poll cycle retries this alert — one bad
                    // alert (e.g. an SMTP outage) must not block the rest of the batch from being sent.
                    _logger.LogError(ex, "Failed to send email notification for alert {AlertId}.", alert.Id);
                }
            }
        }
    }
}
