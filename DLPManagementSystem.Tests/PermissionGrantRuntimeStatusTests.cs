using DLPManagementSystem.Common;

namespace DLPManagementSystem.Tests;

public class PermissionGrantRuntimeStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Revoked_TakesPriorityOverEverythingElse()
    {
        var status = PermissionGrantRuntimeStatus.Compute(
            revokedAtUtc: Now.AddMinutes(-1),
            expiresAtUtc: Now.AddHours(1),
            startsAtUtc: Now.AddHours(-1),
            nowUtc: Now);

        Assert.Equal("Revoked", status);
    }

    [Fact]
    public void Expired_WhenExpiresAtUtcIsAtOrBeforeNow()
    {
        var status = PermissionGrantRuntimeStatus.Compute(
            revokedAtUtc: null,
            expiresAtUtc: Now,
            startsAtUtc: Now.AddHours(-1),
            nowUtc: Now);

        Assert.Equal("Expired", status);
    }

    [Fact]
    public void Pending_WhenStartsAtUtcIsInTheFuture()
    {
        var status = PermissionGrantRuntimeStatus.Compute(
            revokedAtUtc: null,
            expiresAtUtc: null,
            startsAtUtc: Now.AddMinutes(1),
            nowUtc: Now);

        Assert.Equal("Pending", status);
    }

    [Fact]
    public void Active_WhenStartedNotExpiredNotRevoked()
    {
        var status = PermissionGrantRuntimeStatus.Compute(
            revokedAtUtc: null,
            expiresAtUtc: Now.AddHours(1),
            startsAtUtc: Now.AddHours(-1),
            nowUtc: Now);

        Assert.Equal("Active", status);
    }

    [Fact]
    public void Active_WhenExpiresAtUtcIsNull()
    {
        var status = PermissionGrantRuntimeStatus.Compute(
            revokedAtUtc: null,
            expiresAtUtc: null,
            startsAtUtc: Now.AddHours(-1),
            nowUtc: Now);

        Assert.Equal("Active", status);
    }

    [Fact]
    public void StartsAtUtcExactlyNow_IsActiveNotPending()
    {
        var status = PermissionGrantRuntimeStatus.Compute(
            revokedAtUtc: null,
            expiresAtUtc: null,
            startsAtUtc: Now,
            nowUtc: Now);

        Assert.Equal("Active", status);
    }
}
