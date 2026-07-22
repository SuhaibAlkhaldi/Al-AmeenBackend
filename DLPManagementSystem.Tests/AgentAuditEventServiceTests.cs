using DLPManagementSystem.DTO.AgentAuditEvents;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Service;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Tests;

// Regression coverage for Root Cause B: a Block audit event must not raise a stale Alert once the
// permission grant it references has become Active by the time the event is actually received
// (e.g. it was queued client-side around the time of approval and delivered late). The
// underlying AuditEvent must still be persisted either way — this only ever suppresses the Alert.
public sealed class AgentAuditEventServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid DeviceId = Guid.NewGuid();
    private static readonly Guid GrantedByUserId = Guid.NewGuid();

    [Fact]
    public async Task BlockEvent_ReferencingCurrentlyActiveGrant_CreatesAuditEventButNoAlert()
    {
        await using var db = CreateContext();
        var grantId = Guid.NewGuid();
        await SeedLookupsAsync(db);
        await SeedDeviceAsync(db);
        db.PermissionGrants.Add(new PermissionGrant
        {
            Id = grantId,
            OrganizationId = OrganizationId,
            ActionKey = "screen.capture",
            DecisionId = await DecisionIdAsync(db, "Allow"),
            SubjectTypeId = await SubjectTypeIdAsync(db),
            SubjectId = DeviceId.ToString(),
            GrantTypeId = await GrantTypeIdAsync(db),
            Priority = 500,
            StartsAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            Reason = "Approved",
            GrantedByUserId = GrantedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        var service = new AgentAuditEventService(db);
        var request = BuildBlockBatchRequest(grantId);

        var response = await service.ReceiveAuditEventBatchAsync(OrganizationId, DeviceId, request);

        Assert.True(response.Success);
        Assert.Single(response.Data!.AcceptedEventIds);
        Assert.Equal(1, await db.AuditEvents.CountAsync());
        Assert.Empty(await db.Alerts.ToListAsync());
    }

    [Fact]
    public async Task BlockEvent_ReferencingNoGrant_CreatesAuditEventAndAlert()
    {
        await using var db = CreateContext();
        await SeedLookupsAsync(db);
        await SeedDeviceAsync(db);

        var service = new AgentAuditEventService(db);
        var request = BuildBlockBatchRequest(permissionGrantId: null);

        var response = await service.ReceiveAuditEventBatchAsync(OrganizationId, DeviceId, request);

        Assert.True(response.Success);
        Assert.Single(response.Data!.AcceptedEventIds);
        Assert.Equal(1, await db.AuditEvents.CountAsync());
        Assert.Single(await db.Alerts.ToListAsync());
    }

    [Fact]
    public async Task BlockEvent_ReferencingRevokedGrant_CreatesAuditEventAndAlert()
    {
        await using var db = CreateContext();
        var grantId = Guid.NewGuid();
        await SeedLookupsAsync(db);
        await SeedDeviceAsync(db);
        db.PermissionGrants.Add(new PermissionGrant
        {
            Id = grantId,
            OrganizationId = OrganizationId,
            ActionKey = "screen.capture",
            DecisionId = await DecisionIdAsync(db, "Allow"),
            SubjectTypeId = await SubjectTypeIdAsync(db),
            SubjectId = DeviceId.ToString(),
            GrantTypeId = await GrantTypeIdAsync(db),
            Priority = 500,
            StartsAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            RevokedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            Reason = "Approved then revoked",
            GrantedByUserId = GrantedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        var service = new AgentAuditEventService(db);
        var request = BuildBlockBatchRequest(grantId);

        var response = await service.ReceiveAuditEventBatchAsync(OrganizationId, DeviceId, request);

        Assert.True(response.Success);
        Assert.Equal(1, await db.AuditEvents.CountAsync());
        Assert.Single(await db.Alerts.ToListAsync());
    }

    private static AgentAuditBatchRequestDto BuildBlockBatchRequest(Guid? permissionGrantId)
    {
        return new AgentAuditBatchRequestDto
        {
            TenantId = OrganizationId,
            DeviceId = DeviceId,
            AgentVersion = "3.0.0",
            Events =
            [
                new SecurityEventEnvelopeDto
                {
                    EventId = Guid.NewGuid(),
                    CorrelationId = Guid.NewGuid(),
                    ProtocolVersion = "1.0",
                    EventSchemaVersion = "1.0",
                    TenantId = OrganizationId,
                    DeviceId = DeviceId,
                    ActionKey = "screen.capture",
                    EventType = "ScreenshotToolBlocked",
                    Decision = "Block",
                    ReasonCode = "ScreenshotProcessDeniedByPolicy",
                    PermissionGrantId = permissionGrantId,
                    OccurredAtUtc = DateTimeOffset.UtcNow.AddSeconds(-30),
                    AgentVersion = "3.0.0",
                    IntegrityHash = ""
                }
            ]
        };
    }

    private static DLPSystemContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DLPSystemContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new DLPSystemContext(options);
    }

    private static async Task SeedDeviceAsync(DLPSystemContext db)
    {
        db.Devices.Add(new Device
        {
            Id = DeviceId,
            OrganizationId = OrganizationId,
            DeviceKey = "test-device",
            MachineName = "TEST-PC",
            StatusId = 1,
            CurrentPolicyVersion = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedLookupsAsync(DLPSystemContext db)
    {
        db.AuditDecisions.AddRange(
            new AuditDecision { Id = 1, Name = "Allow", DisplayName = "Allow" },
            new AuditDecision { Id = 2, Name = "Block", DisplayName = "Block" },
            new AuditDecision { Id = 3, Name = "AuditOnly", DisplayName = "Audit Only" },
            new AuditDecision { Id = 4, Name = "Error", DisplayName = "Error" });

        db.AuditEventTypes.AddRange(
            new AuditEventType { Id = 1, Name = "ActionAllowed", DisplayName = "Action Allowed" },
            new AuditEventType { Id = 2, Name = "ActionBlocked", DisplayName = "Action Blocked" },
            new AuditEventType { Id = 3, Name = "PermissionEvaluated", DisplayName = "Permission Evaluated" },
            new AuditEventType { Id = 4, Name = "ScreenshotToolBlocked", DisplayName = "Screenshot Tool Blocked" });

        db.AuditReasonCodes.Add(new AuditReasonCode { Id = 1, Code = "ScreenshotProcessDeniedByPolicy", DisplayName = "Denied by policy" });

        db.AlertStatuses.Add(new AlertStatus { Id = 1, Name = "New" });
        db.AlertLevels.Add(new AlertLevel { Id = 1, Name = "High", MinRiskScore = 0, MaxRiskScore = 100 });

        db.PermissionSubjectTypes.Add(new PermissionSubjectType { Id = 1, Name = "DeviceId", DisplayName = "Device" });
        db.PermissionGrantTypes.Add(new PermissionGrantType { Id = 1, Name = "Temporary", DisplayName = "Temporary" });
        db.PermissionDecisions.AddRange(
            new PermissionDecision { Id = 1, Name = "Allow", DisplayName = "Allow" },
            new PermissionDecision { Id = 2, Name = "Deny", DisplayName = "Deny" });

        db.PermissionActionCategories.Add(new PermissionActionCategory { Id = 1, Name = "Screen", DisplayName = "Screen" });
        db.PermissionActions.Add(new PermissionAction
        {
            Key = "screen.capture",
            CategoryId = 1,
            DisplayName = "Screen Capture",
            DefaultDecisionId = 1,
            SupportsPermanentGrant = true,
            SupportsTemporaryGrant = true,
            IsEnabled = true,
            SortOrder = 1
        });

        await db.SaveChangesAsync();
    }

    private static async Task<int> DecisionIdAsync(DLPSystemContext db, string name) =>
        (await db.PermissionDecisions.FirstOrDefaultAsync(x => x.Name == name))?.Id
            ?? throw new InvalidOperationException($"Seed a PermissionDecisions row named '{name}' first.");

    private static async Task<int> SubjectTypeIdAsync(DLPSystemContext db) =>
        (await db.PermissionSubjectTypes.FirstAsync()).Id;

    private static async Task<int> GrantTypeIdAsync(DLPSystemContext db) =>
        (await db.PermissionGrantTypes.FirstAsync()).Id;
}
