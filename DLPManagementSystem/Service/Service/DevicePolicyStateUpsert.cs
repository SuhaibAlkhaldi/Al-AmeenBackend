using DLPManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    // DevicePolicyStates has a single row per device (DeviceId is the primary key). Two
    // near-simultaneous requests for the same never-before-synced device (e.g. the agent's
    // startup policy fetch racing its background sync worker) can both see "no row exists"
    // before either commits, so both try to INSERT and the loser hits a PK violation.
    // This performs the same get-or-create, but retries once as an UPDATE against the
    // row the other request won instead of letting that violation bubble up.
    internal static class DevicePolicyStateUpsert
    {
        public static async Task ApplyAsync(
            DLPSystemContext db,
            Guid deviceId,
            Guid organizationId,
            Action<DevicePolicyState> applyChanges,
            CancellationToken cancellationToken)
        {
            var policyState = await db.DevicePolicyStates
                .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

            var isNewRow = policyState == null;

            if (isNewRow)
            {
                policyState = new DevicePolicyState
                {
                    DeviceId = deviceId,
                    OrganizationId = organizationId
                };

                db.DevicePolicyStates.Add(policyState);
            }

            applyChanges(policyState!);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (isNewRow)
            {
                // Lost the insert race. Detach our failed Added entity and update the row
                // the concurrent request already committed instead.
                db.Entry(policyState!).State = EntityState.Detached;

                var existingPolicyState = await db.DevicePolicyStates
                    .FirstAsync(x => x.DeviceId == deviceId, cancellationToken);

                applyChanges(existingPolicyState);

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
