using DLPManagementSystem.Helper.Email;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DLPManagementSystem.Service.Service
{
    public class DemoRequestEmailService : IDemoRequestEmailService
    {
        // Ameen's own sales inbox — a fixed business fact (same address already published on the
        // landing page's footer/contact section), not a per-deployment setting.
        private const string RecipientEmail = "Info@ameen-dlp.com";
        private const string RecipientName = "AMEEN Sales";

        private readonly SmtpOptions _options;
        private readonly ILogger<DemoRequestEmailService> _logger;

        public DemoRequestEmailService(IOptions<SmtpOptions> options, ILogger<DemoRequestEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendNewDemoRequestNotificationAsync(DemoRequest demoRequest, CancellationToken cancellationToken = default)
        {
            var subject = $"[AMEEN] New demo request — {demoRequest.CompanyName}";
            var body = BuildBody(demoRequest);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(new MailboxAddress(RecipientName, RecipientEmail));
            message.ReplyTo.Add(new MailboxAddress(demoRequest.FullName, demoRequest.CompanyEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            if (!_options.IsConfigured)
            {
                _logger.LogInformation(
                    "SMTP not configured (Smtp:Host empty) — would-be demo request email logged instead of sent. To: {Recipient}, Subject: {Subject}, Body: {Body}",
                    RecipientEmail, subject, body);
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

        private static string BuildBody(DemoRequest demoRequest)
        {
            return
                $"""
                A new "Request a Demo" submission was received on the AMEEN landing page.

                Full name: {demoRequest.FullName}
                Company email: {demoRequest.CompanyEmail}
                Company name: {demoRequest.CompanyName}
                Company size: {demoRequest.CompanySize}
                Phone: {demoRequest.Phone ?? "N/A"}
                Submitted at (UTC): {demoRequest.CreatedAtUtc:u}

                Open the Al-Ameen admin dashboard → Demo Requests to follow up.
                """;
        }
    }
}
