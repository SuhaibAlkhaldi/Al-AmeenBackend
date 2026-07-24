namespace DLPManagementSystem.Helper.Email
{
    public sealed class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;

        // No host configured means "dev mode": the send path logs the would-be email instead of
        // attempting a real SMTP connection, mirroring this project's other dev-mode fallbacks.
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
    }
}
