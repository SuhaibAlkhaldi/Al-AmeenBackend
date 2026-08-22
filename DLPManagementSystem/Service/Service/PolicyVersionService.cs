using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DLPManagementSystem.Service.Service
{
    // Stages a new PolicyVersion + PolicyChangeLog row so the next agent policy poll for this org
    // picks up a change (grant approved/revoked, device reassigned, etc.). Callers must still call
    // SaveChangesAsync themselves — this only adds to the tracked context, it doesn't persist alone,
    // so the version bump commits atomically together with whatever change triggered it.
    public sealed class PolicyVersionService : IPolicyVersionService
    {
        private readonly DLPSystemContext _db;

        public PolicyVersionService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task BumpAsync(
            Guid organizationId,
            Guid? changedByUserId,
            string changeType,
            string entityType,
            Guid? entityId,
            string description,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTimeOffset.UtcNow;

            // Was previously "read MAX(VersionNumber), add 1 in memory" - two concurrent callers for
            // the same organization (e.g. a device assignment and a permission grant landing at the
            // same instant) could both read the same max and then both try to insert the same next
            // number, tripping UQ_PolicyVersions_Organization_Version and surfacing as an unhandled
            // 500 to a caller whose operation was otherwise entirely valid. NEXT VALUE FOR is a single
            // atomic, lock-free database operation - two concurrent callers are physically incapable
            // of getting the same value back, so the unique-index collision this used to cause can no
            // longer happen. One sequence shared across every organization rather than one per
            // organization - see DLPSystemContext.OnModelCreating's HasSequence call for why.
            //
            // Raw ADO.NET rather than Database.SqlQuery<long>(...).SingleAsync() - SQL Server rejects
            // NEXT VALUE FOR inside a subquery/derived table (error 11719), and SqlQuery composed with
            // any further LINQ operator (including SingleAsync) wraps the raw SQL in exactly that. This
            // goes through the same connection EF Core already owns (opened via OpenConnectionAsync,
            // which ref-counts rather than stealing the connection out from under EF) and joins whatever
            // transaction the caller may already have open via Database.CurrentTransaction, so it stays
            // part of the same unit of work as everything else BumpAsync's caller does.
            long nextVersionNumber;
            var connection = _db.Database.GetDbConnection();
            await _db.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT NEXT VALUE FOR dbo.PolicyVersionNumbers";
                if (_db.Database.CurrentTransaction != null)
                {
                    command.Transaction = _db.Database.CurrentTransaction.GetDbTransaction();
                }

                nextVersionNumber = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
            }
            finally
            {
                await _db.Database.CloseConnectionAsync();
            }

            var policyVersion = new PolicyVersion
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                VersionNumber = nextVersionNumber,
                ChangedAtUtc = nowUtc,
                ChangedByUserId = changedByUserId,
                ChangeReason = description
            };

            _db.PolicyVersions.Add(policyVersion);

            _db.PolicyChangeLogs.Add(new PolicyChangeLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                PolicyVersionId = policyVersion.Id,
                ChangeType = changeType,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                ChangedByUserId = changedByUserId,
                ChangedAtUtc = nowUtc
            });
        }
    }
}
