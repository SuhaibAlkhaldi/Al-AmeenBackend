using System.Text.Json;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentAuditEvents;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DLPManagementSystem.Service.Service
{
    public class AgentAuditEventService : IAgentAuditEventService
    {
        // The agent's SecurityEventEnvelope.Decision enum ("Allow"/"Block"/"Audit"/"Error") does not use the
        // exact same names as the AuditDecisions lookup table ("Audit" -> "AuditOnly"); translate here.
        private static readonly Dictionary<string, string> DecisionNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Allow"] = "Allow",
            ["Block"] = "Block",
            ["Audit"] = "AuditOnly",
            ["Error"] = "Error"
        };

        private readonly DLPSystemContext _db;
        private readonly ILogger<AgentAuditEventService> _logger;

        public AgentAuditEventService(DLPSystemContext db, ILogger<AgentAuditEventService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ApiResponse<AgentAuditBatchResultDto>> ReceiveAuditEventBatchAsync(
            Guid organizationId,
            Guid deviceId,
            AgentAuditBatchRequestDto request,
            JsonElement rawBody,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Correlated with request.Events by array index below - System.Text.Json binds a JSON
                // array to a List<T> element-for-element in order (a malformed element fails the whole
                // bind rather than being silently dropped), so index correlation is safe. Falls back to
                // an empty list - not a thrown exception - if "events" is missing/not an array for any
                // reason, since a raw-body shape surprise shouldn't take down ingestion; every event just
                // gets IntegrityVerified = null (not evaluated) in that case.
                var rawEvents = rawBody.TryGetProperty("events", out var rawEventsProperty)
                    && rawEventsProperty.ValueKind == JsonValueKind.Array
                        ? rawEventsProperty.EnumerateArray().ToList()
                        : new List<JsonElement>();
                if (request.TenantId != organizationId || request.DeviceId != deviceId)
                {
                    return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                        "tenantId/deviceId do not match the authenticated device.",
                        "معرّف المؤسسة أو الجهاز لا يطابق الجهاز المصادق عليه");
                }

                if (request.Events == null || request.Events.Count == 0)
                {
                    return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                        "At least one audit event is required.",
                        "يجب إرسال حدث واحد على الأقل");
                }

                if (request.Events.Count > 500)
                {
                    return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                        "A batch cannot contain more than 500 events.",
                        "لا يمكن أن تحتوي الدفعة على أكثر من 500 حدث");
                }

                var device = await _db.Devices
                    .FirstOrDefaultAsync(x => x.Id == deviceId && x.OrganizationId == organizationId, cancellationToken);

                if (device == null)
                {
                    return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                        "Device was not found.",
                        "الجهاز غير مسجل في النظام");
                }

                var result = new AgentAuditBatchResultDto();

                var incomingEventIds = request.Events.Select(x => x.EventId).ToList();

                var existingEventIdSet = (await _db.AuditEvents
                        .Where(x => incomingEventIds.Contains(x.Id))
                        .Select(x => x.Id)
                        .ToListAsync(cancellationToken))
                    .ToHashSet();

                var decisionMap = await _db.AuditDecisions
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var eventTypeMap = await _db.AuditEventTypes
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var reasonCodeMap = await _db.AuditReasonCodes
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var validActionKeySet = (await _db.PermissionActions
                        .AsNoTracking()
                        .Select(x => x.Key)
                        .ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var userSids = request.Events
                    .Where(x => !string.IsNullOrWhiteSpace(x.UserSid))
                    .Select(x => x.UserSid!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var employeeBySid = await _db.EmployeeWindowsIdentities
                    .AsNoTracking()
                    .Where(x => userSids.Contains(x.UserSid) && x.RevokedAtUtc == null)
                    .GroupBy(x => x.UserSid)
                    .Select(g => new { UserSid = g.Key, EmployeeId = g.First().EmployeeId })
                    .ToDictionaryAsync(x => x.UserSid, x => x.EmployeeId, StringComparer.OrdinalIgnoreCase, cancellationToken);

                // Nothing in this app today actually populates EmployeeWindowsIdentities (no admin UI,
                // no self-service flow) - the device's currently assigned employee (DeviceUserAssignments,
                // which the portal's device detail page already manages) is the realistic, already-working
                // source of truth for "which employee is this device's audit event about", so fall back to
                // it whenever the SID isn't explicitly mapped.
                var assignedEmployeeId = await _db.DeviceUserAssignments
                    .AsNoTracking()
                    .Where(x => x.DeviceId == deviceId && x.UnassignedAtUtc == null)
                    .Select(x => (Guid?)x.EmployeeId)
                    .FirstOrDefaultAsync(cancellationToken);

                var permissionGrantIds = request.Events
                    .Where(x => x.PermissionGrantId.HasValue)
                    .Select(x => x.PermissionGrantId!.Value)
                    .Distinct()
                    .ToList();

                var existingGrantIdSet = permissionGrantIds.Count == 0
                    ? new HashSet<Guid>()
                    : (await _db.PermissionGrants
                            .AsNoTracking()
                            .Where(x => permissionGrantIds.Contains(x.Id))
                            .Select(x => x.Id)
                            .ToListAsync(cancellationToken))
                        .ToHashSet();

                // Every Block decision raises an alert (see below). Resolve the "New" status and the
                // "High" level by name once, the same way the other lookup maps above are resolved,
                // so this doesn't depend on seeded ids staying stable.
                var newAlertStatusId = await _db.AlertStatuses
                    .AsNoTracking()
                    .Where(x => x.Name == "New")
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                // TODO: derive AlertLevel from a real risk score once risk scoring exists. For now every
                // Block decision is treated as "High" since the agent only sends a Decision enum, not a
                // score. AuditOnly decisions could reasonably generate lower-severity alerts later.
                var highAlertLevelId = await _db.AlertLevels
                    .AsNoTracking()
                    .Where(x => x.Name == "High")
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var canCreateAlerts = newAlertStatusId.HasValue && highAlertLevelId.HasValue;

                // A flagged integrity mismatch is more significant than an ordinary Block decision - it
                // suggests a compromised or malfunctioning agent, not just a normal policy denial - so it
                // always raises a Critical alert regardless of the event's own decision, independent of
                // (and in addition to) the Block-decision alert below.
                var criticalAlertLevelId = await _db.AlertLevels
                    .AsNoTracking()
                    .Where(x => x.Name == "Critical")
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var canCreateCriticalAlerts = newAlertStatusId.HasValue && criticalAlertLevelId.HasValue;

                var nowUtc = DateTimeOffset.UtcNow;
                var seenInBatch = new HashSet<Guid>();

                for (var eventIndex = 0; eventIndex < request.Events.Count; eventIndex++)
                {
                    var envelope = request.Events[eventIndex];

                    if (existingEventIdSet.Contains(envelope.EventId) || !seenInBatch.Add(envelope.EventId))
                    {
                        result.DuplicateEventIds.Add(envelope.EventId);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(envelope.ActionKey) || !validActionKeySet.Contains(envelope.ActionKey))
                    {
                        result.RejectedEvents.Add(new RejectedAuditEventDto
                        {
                            EventId = envelope.EventId,
                            ReasonCode = "UnknownActionKey",
                            Retryable = false
                        });
                        continue;
                    }

                    if (!DecisionNameMap.TryGetValue(envelope.Decision ?? string.Empty, out var mappedDecisionName) ||
                        !decisionMap.TryGetValue(mappedDecisionName, out var decisionId))
                    {
                        result.RejectedEvents.Add(new RejectedAuditEventDto
                        {
                            EventId = envelope.EventId,
                            ReasonCode = "UnknownDecision",
                            Retryable = false
                        });
                        continue;
                    }

                    if (!eventTypeMap.TryGetValue(envelope.EventType ?? string.Empty, out var eventTypeId))
                    {
                        var fallbackEventTypeName = mappedDecisionName switch
                        {
                            "Allow" => "ActionAllowed",
                            "AuditOnly" => "PermissionEvaluated",
                            _ => "ActionBlocked"
                        };

                        eventTypeId = eventTypeMap[fallbackEventTypeName];
                    }

                    int? reasonCodeId = null;
                    if (!string.IsNullOrWhiteSpace(envelope.ReasonCode) &&
                        reasonCodeMap.TryGetValue(envelope.ReasonCode, out var mappedReasonCodeId))
                    {
                        reasonCodeId = mappedReasonCodeId;
                    }

                    Guid? employeeId = null;
                    if (!string.IsNullOrWhiteSpace(envelope.UserSid) &&
                        employeeBySid.TryGetValue(envelope.UserSid, out var foundEmployeeId))
                    {
                        employeeId = foundEmployeeId;
                    }
                    else
                    {
                        employeeId = assignedEmployeeId;
                    }

                    Guid? permissionGrantId = envelope.PermissionGrantId.HasValue &&
                        existingGrantIdSet.Contains(envelope.PermissionGrantId.Value)
                            ? envelope.PermissionGrantId
                            : null;

                    // Verified against the RAW wire bytes for this event (see AuditIntegrityVerifier),
                    // not a reconstructed SecurityEventEnvelopeDto - deliberately sidesteps the DTO-parity
                    // bug class already found once for policy signing. Per product decision: a mismatch
                    // never blocks ingestion, it's just flagged (IntegrityVerified = false) for review.
                    var integrityVerified = eventIndex < rawEvents.Count
                        ? AuditIntegrityVerifier.Verify(rawEvents[eventIndex])
                        : null;

                    // Fields without a dedicated column on AuditEvent are preserved here for full fidelity.
                    var metadata = new
                    {
                        rawEventType = envelope.EventType,
                        ruleId = envelope.RuleId,
                        policyId = envelope.PolicyId,
                        windowsSessionId = envelope.WindowsSessionId,
                        userId = envelope.UserId,
                        sourceProcess = envelope.SourceProcess,
                        resource = envelope.Resource,
                        destination = envelope.Destination,
                        details = envelope.Details,
                        protocolVersion = envelope.ProtocolVersion,
                        eventSchemaVersion = envelope.EventSchemaVersion,
                        osVersion = envelope.OsVersion,
                        isDevelopmentEvent = envelope.IsDevelopmentEvent,
                        integrityHash = envelope.IntegrityHash,
                        rawReasonCode = reasonCodeId == null ? envelope.ReasonCode : null,
                        rawPermissionGrantId = permissionGrantId == null ? envelope.PermissionGrantId : null
                    };

                    var auditEvent = new AuditEvent
                    {
                        Id = envelope.EventId,
                        OrganizationId = organizationId,
                        DeviceId = deviceId,
                        EmployeeId = employeeId,
                        UserSid = string.IsNullOrWhiteSpace(envelope.UserSid) ? null : envelope.UserSid,
                        Username = string.IsNullOrWhiteSpace(envelope.Username) ? null : envelope.Username,
                        ActionKey = envelope.ActionKey,
                        EventTypeId = eventTypeId,
                        DecisionId = decisionId,
                        ReasonCodeId = reasonCodeId,
                        PermissionGrantId = permissionGrantId,
                        PolicyVersion = envelope.PolicyVersion,
                        OccurredAtUtc = envelope.OccurredAtUtc,
                        ReceivedAtUtc = nowUtc,
                        AgentVersion = envelope.AgentVersion,
                        CorrelationId = envelope.CorrelationId,
                        IntegrityVerified = integrityVerified,
                        MetadataJson = JsonSerializer.Serialize(metadata)
                    };

                    _db.AuditEvents.Add(auditEvent);

                    // Only Block decisions raise an alert for now — that's the only decision this
                    // system can currently justify surfacing to an admin. Allow/AuditOnly/Error don't.
                    if (mappedDecisionName == "Block" && canCreateAlerts)
                    {
                        _db.Alerts.Add(new Alert
                        {
                            OrganizationId = organizationId,
                            AuditEventId = auditEvent.Id,
                            AlertLevelId = highAlertLevelId!.Value,
                            AlertStatusId = newAlertStatusId!.Value,
                            Title = $"Blocked: {envelope.ActionKey}",
                            Description = string.IsNullOrWhiteSpace(envelope.ReasonCode) ? null : $"Reason: {envelope.ReasonCode}",
                            CreatedAtUtc = nowUtc
                        });
                    }

                    // Independent of the Block-decision alert above - an integrity mismatch can co-occur
                    // with any decision (or none). Always Critical: this signals the event itself may not
                    // be trustworthy, not just that a policy denied something.
                    if (integrityVerified == false && canCreateCriticalAlerts)
                    {
                        _db.Alerts.Add(new Alert
                        {
                            OrganizationId = organizationId,
                            AuditEventId = auditEvent.Id,
                            AlertLevelId = criticalAlertLevelId!.Value,
                            AlertStatusId = newAlertStatusId!.Value,
                            Title = $"Integrity check failed: {envelope.ActionKey}",
                            Description = "This audit event's integrityHash did not match the recomputed hash of its received data - the event was still recorded, but its authenticity could not be verified. This may indicate a compromised or malfunctioning agent.",
                            CreatedAtUtc = nowUtc
                        });
                    }

                    result.AcceptedEventIds.Add(envelope.EventId);
                }

                device.LastSeenAtUtc = nowUtc;
                device.AgentVersion = request.AgentVersion;
                device.UpdatedAtUtc = nowUtc;

                await _db.SaveChangesAsync(cancellationToken);

                return ApiResponse<AgentAuditBatchResultDto>.SuccessResponse(
                    result,
                    "Audit event batch processed.",
                    "تمت معالجة دفعة الأحداث");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Unexpected error while receiving an audit event batch for organization {OrganizationId}, device {DeviceId}.",
                    organizationId, deviceId);
                return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                    "Unexpected error occurred while receiving audit events.",
                    "حدث خطأ غير متوقع أثناء استلام الأحداث");
            }
        }
    }
}
