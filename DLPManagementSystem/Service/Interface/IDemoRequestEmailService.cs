using DLPManagementSystem.Models;

namespace DLPManagementSystem.Service.Interface
{
    public interface IDemoRequestEmailService
    {
        // Notifies the sales inbox of a new demo request. Throws on send failure so the caller can
        // decide how to handle it (DemoRequestService treats this as best-effort and swallows it —
        // a broken SMTP config must never fail the public form submission itself).
        Task SendNewDemoRequestNotificationAsync(DemoRequest demoRequest, CancellationToken cancellationToken = default);
    }
}
