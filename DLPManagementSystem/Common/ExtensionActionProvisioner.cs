using System.Text.RegularExpressions;
using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Common
{
    // Browser extension IDs are arbitrary and unbounded (chosen live by whichever employee requests
    // one, or by an admin granting one proactively) - unlike every other PermissionAction, which is a
    // small fixed catalog seeded once in DatabaseSeeder.cs. This provisions the one PermissionAction
    // row a given extension ID needs the first time anyone references it, instead of relaxing the
    // existing "ActionKey must already exist" validation that PermissionRequestService.CreateAsync and
    // PermissionGrantService.CreateDirectGrantAsync both still correctly enforce for every other,
    // fixed-catalog action key.
    public static class ExtensionActionProvisioner
    {
        // Chrome/Edge Web Store extension IDs are always exactly 32 lowercase letters a-p (base16 using
        // that alphabet instead of 0-9a-f) - see Chrome's own "key" manifest field documentation.
        private static readonly Regex ExtensionIdPattern = new("^[a-p]{32}$", RegexOptions.Compiled);

        public static bool IsValidExtensionId(string? extensionId) =>
            !string.IsNullOrWhiteSpace(extensionId) && ExtensionIdPattern.IsMatch(extensionId);

        public static string BuildActionKey(string extensionId) =>
            PermissionActionKeys.ExtensionActionKeyPrefix + extensionId;

        // Idempotent and race-tolerant: if two requests for the same brand-new extension ID land at
        // once, the loser's insert fails the Key unique constraint, which is swallowed here since the
        // row exists either way by the time this returns. Saves immediately (rather than leaving the
        // new row for the caller's own later SaveChangesAsync) so this row is durable before the
        // caller's PermissionRequest/PermissionGrant insert, which has a real FK to it, is attempted.
        public static async Task EnsureActionExistsAsync(
            DLPSystemContext db, string extensionId, CancellationToken cancellationToken)
        {
            var key = BuildActionKey(extensionId);
            var exists = await db.PermissionActions.AnyAsync(x => x.Key == key, cancellationToken);
            if (exists) return;

            var category = await db.PermissionActionCategories
                .FirstAsync(x => x.Name == "Extensions", cancellationToken);
            var denyDecision = await db.PermissionDecisions
                .FirstAsync(x => x.Name == "Deny", cancellationToken);

            var action = new PermissionAction
            {
                Key = key,
                CategoryId = category.Id,
                DisplayName = extensionId,
                Description = "Browser extension approval, provisioned on demand when first requested or granted.",
                DefaultDecisionId = denyDecision.Id,
                SupportsPermanentGrant = true,
                SupportsTemporaryGrant = true,
                IsEnabled = true,
                SortOrder = 500
            };

            db.PermissionActions.Add(action);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (DbExceptionHelper.IsUniqueConstraintViolation(ex))
            {
                db.Entry(action).State = EntityState.Detached;
            }
        }
    }
}
