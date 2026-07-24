using DLPManagementSystem.Models;

namespace DLPManagementSystem.Service.Interface
{
    public interface IPasswordService
    {
        // Hashes a new password with the current (Microsoft.AspNetCore.Identity) scheme. Used whenever a
        // password is being set/reset, not verified against an existing value.
        string HashPassword(User user, string password);

        // Verifies suppliedPassword against user.PasswordHash. Transparently handles both formats:
        // - Legacy raw-SHA256-hex (pre-migration accounts): verified via the old comparison, and on success
        //   user.PasswordHash is rewritten in place to the new format (caller must still SaveChangesAsync).
        // - Current-format hash: verified via PasswordHasher, rewritten in place if the hasher signals its
        //   own parameters should be upgraded (SuccessRehashNeeded).
        // Returns false without ever mutating user.PasswordHash if the password is wrong.
        bool VerifyAndUpgrade(User user, string suppliedPassword);
    }
}
