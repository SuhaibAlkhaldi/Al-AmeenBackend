using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using DLPManagementSystem.Tests.TestHelpers;

namespace DLPManagementSystem.Tests.Service
{
    // Covers the ui-data-batch2 addition: PermissionAction.IsSensitive surfacing correctly on
    // PermissionActionDto (GetPermissionActionsAsync) - the lookups endpoint the frontend's
    // employee-permissions page reads its action catalog from.
    public class LookupsServiceTests
    {
        [Fact]
        public async Task GetPermissionActionsAsync_ReflectsIsSensitiveTrueAndFalse()
        {
            var db = TestDbContextFactory.Create();
            // "usb.storage" is already seeded by TestDbContextFactory with IsSensitive left at its
            // default (false) - add one more action, explicitly marked sensitive, to assert both
            // values come through rather than just the default.
            db.PermissionActions.Add(new PermissionAction
            {
                Key = "file.decrypt",
                CategoryId = 3, // "Screen" - category seeded by TestDbContextFactory, irrelevant here
                DisplayName = "File Decrypt",
                DefaultDecisionId = 2,
                SupportsPermanentGrant = true,
                SupportsTemporaryGrant = true,
                IsEnabled = true,
                SortOrder = 130,
                IsSensitive = true
            });
            await db.SaveChangesAsync();

            var service = new LookupsService(db);
            var actions = await service.GetPermissionActionsAsync();

            var usbStorage = Assert.Single(actions, a => a.Key == "usb.storage");
            Assert.False(usbStorage.IsSensitive);

            var fileDecrypt = Assert.Single(actions, a => a.Key == "file.decrypt");
            Assert.True(fileDecrypt.IsSensitive);
        }
    }
}
