using DLPManagementSystem.Models;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAlertEmailService
    {
        // Sends one notification email per recipient for the given alert. Throws on send failure so the
        // caller (the background worker) can decide how to handle it — this service does not swallow
        // errors itself.
        Task SendAlertNotificationAsync(Alert alert, IEnumerable<User> recipients, CancellationToken cancellationToken = default);
    }
}
