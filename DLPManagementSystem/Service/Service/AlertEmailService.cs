using DLPManagementSystem.Helper.Email;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DLPManagementSystem.Service.Service
{
    public class AlertEmailService : IAlertEmailService
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<AlertEmailService> _logger;

        public AlertEmailService(IOptions<SmtpOptions> options, ILogger<AlertEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAlertNotificationAsync(Alert alert, IEnumerable<User> recipients, CancellationToken cancellationToken = default)
        {
            var recipientList = recipients.ToList();

            if (recipientList.Count == 0)
            {
                return;
            }

            var subject = $"[DLP Alert] {alert.Title}";
            var body = BuildBody(alert);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));

            foreach (var recipient in recipientList)
            {
                message.To.Add(new MailboxAddress(recipient.FullName, recipient.Email));
            }

            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            if (!_options.IsConfigured)
            {
                // Dev-mode fallback: no Smtp:Host configured, so log the would-be email instead of
                // attempting a real connection, rather than crashing or silently no-op'ing with no trace.
                _logger.LogInformation(
                    "SMTP not configured (Smtp:Host empty) — would-be email logged instead of sent. To: {Recipients}, Subject: {Subject}, Body: {Body}",
                    string.Join(", ", recipientList.Select(x => x.Email)), subject, body);
                return;
            }

            using var client = new SmtpClient();

            var secureSocketOptions = _options.EnableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        private static string BuildBody(Alert alert)
        {
            var auditEvent = alert.AuditEvent;
            var deviceName = auditEvent?.Device?.MachineName ?? "Unknown device";
            var employeeName = auditEvent?.Employee?.DisplayName ?? "Unknown employee";
            var actionKey = auditEvent?.ActionKey ?? "Unknown action";
            var occurredAtUtc = auditEvent?.OccurredAtUtc.ToString("u") ?? "Unknown time";

            return
                $"""
                A {alert.AlertLevel?.Name ?? "High"} severity alert was raised.

                Action: {actionKey}
                Device: {deviceName}
                Employee: {employeeName}
                Reason: {alert.Description ?? "N/A"}
                Occurred At (UTC): {occurredAtUtc}
                Alert Created At (UTC): {alert.CreatedAtUtc:u}

                Open the Al-Ameen dashboard to review and respond to this alert.
                """;
        }
    }
}
