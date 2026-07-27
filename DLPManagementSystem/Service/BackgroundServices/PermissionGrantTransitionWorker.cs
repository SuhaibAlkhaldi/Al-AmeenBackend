using System.Data;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.BackgroundServices;

public sealed class PermissionGrantTransitionWorker(IServiceScopeFactory scopeFactory, ILogger<PermissionGrantTransitionWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PublishTransitionsAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Temporary permission grant transition processing failed."); }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishTransitionsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DLPSystemContext>();
        var nowUtc = DateTimeOffset.UtcNow;
        var temporaryGrants = await db.PermissionGrants.AsNoTracking()
            .Where(g => g.RevokedAtUtc == null && g.ExpiresAtUtc != null)
            .Select(g => new GrantTransition(g.Id, g.OrganizationId, g.ActionKey, g.SubjectId, g.CreatedAtUtc, g.StartsAtUtc, g.ExpiresAtUtc!.Value))
            .ToListAsync(cancellationToken);

        foreach (var grant in temporaryGrants)
        {
            if (grant.StartsAtUtc > grant.CreatedAtUtc && grant.StartsAtUtc <= nowUtc && grant.ExpiresAtUtc > nowUtc)
                await PublishOnceAsync(grant, "GrantActivated", "became active", cancellationToken);

            if (grant.ExpiresAtUtc <= nowUtc)
                await PublishOnceAsync(grant, "GrantExpired", "expired", cancellationToken);
        }
    }

    private async Task PublishOnceAsync(GrantTransition grant, string changeType, string transition, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DLPSystemContext>();
        var versions = scope.ServiceProvider.GetRequiredService<IPolicyVersionService>();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var isStillEligible = await db.PermissionGrants.AnyAsync(g => g.Id == grant.Id && g.RevokedAtUtc == null, cancellationToken);
        if (!isStillEligible) return;
        var exists = await db.PolicyChangeLogs.AnyAsync(log => log.EntityType == "PermissionGrant" && log.EntityId == grant.Id && log.ChangeType == changeType, cancellationToken);
        if (exists) return;

        await versions.BumpAsync(grant.OrganizationId, null, changeType, "PermissionGrant", grant.Id,
            $"Temporary permission '{grant.ActionKey}' {transition} for subject {grant.SubjectId}.", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private sealed record GrantTransition(Guid Id, Guid OrganizationId, string ActionKey, string SubjectId, DateTimeOffset CreatedAtUtc, DateTimeOffset StartsAtUtc, DateTimeOffset ExpiresAtUtc);
}