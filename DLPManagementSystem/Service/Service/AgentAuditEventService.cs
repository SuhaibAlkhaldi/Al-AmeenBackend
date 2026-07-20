using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentAuditEvents;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class AgentAuditEventService : IAgentAuditEventService
    {
        private readonly DLPSystemContext _db;

        public AgentAuditEventService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<AgentAuditBatchResultDto>> ReceiveAuditEvents(
            string deviceKey,
            string agentSecret,AgentAuditBatchRequestDto request,CancellationToken cancellationToken = default)
        {
            try
            {
                var validationError = ValidateRequestHeadersAndBody(
                    deviceKey,
                    agentSecret,
                    request);

                if (validationError != null)
                {
                    return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                        validationError,
                        "البيانات المرسلة غير صحيحة");
                }

                var nowUtc = DateTime.UtcNow;
                var agentSecretHash = SecurityHashHelper.Sha256(agentSecret);

                var device = await _db.Devices
                    .FirstOrDefaultAsync(x => x.DeviceKey == deviceKey, cancellationToken);

                if (device == null)
                {
                    return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                        "Device was not found.",
                        "الجهاز غير مسجل في النظام");
                }

                var credential = await _db.DeviceCredentials
                    .FirstOrDefaultAsync(x =>
                        x.DeviceId == device.Id &&
                        x.RevokedAtUtc == null,
                        cancellationToken);

                if (credential == null || credential.SecretHash != agentSecretHash)
                {
                    return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                        "Invalid device credentials.",
                        "بيانات اعتماد الجهاز غير صحيحة");
                }

                var existingBatch = await _db.AuditEventBatches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BatchId == request.BatchId, cancellationToken);

                if (existingBatch != null)
                {
                    var alreadyProcessedResult = new AgentAuditBatchResultDto
                    {
                        BatchId = request.BatchId,
                        ReceivedEvents = 0,
                        AlreadyProcessed = true
                    };

                    return ApiResponse<AgentAuditBatchResultDto>.SuccessResponse(
                        alreadyProcessedResult,
                        "Audit batch was already processed.",
                        "تمت معالجة هذه الدفعة مسبقًا");
                }

                var decisionMap = await _db.AuditDecisions
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

                var reasonCodeMap = await _db.AuditReasonCodes
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);

                var eventTypeMap = await _db.AuditEventTypes
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);

                var validActionKeys = await _db.PermissionActions
                    .AsNoTracking()
                    .Select(x => x.Key)
                    .ToListAsync(cancellationToken);

                var validActionKeySet = validActionKeys
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var userSids = request.Events
                    .Where(x => !string.IsNullOrWhiteSpace(x.UserSid))
                    .Select(x => x.UserSid)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var employeeBySid = await _db.EmployeeWindowsIdentities
                    .AsNoTracking()
                    .Where(x => userSids.Contains(x.UserSid) && x.RevokedAtUtc == null)
                    .GroupBy(x => x.UserSid)
                    .Select(g => new
                    {
                        UserSid = g.Key,
                        EmployeeId = g.First().EmployeeId
                    })
                    .ToDictionaryAsync(x => x.UserSid, x => x.EmployeeId, cancellationToken);

                foreach (var incomingEvent in request.Events)
                {
                    var eventValidationError = ValidateEvent(
                        incomingEvent,
                        validActionKeySet,
                        decisionMap,
                        reasonCodeMap,
                        eventTypeMap);

                    if (eventValidationError != null)
                    {
                        return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                            eventValidationError,
                            "بيانات الحدث غير صحيحة");
                    }
                }

                var batch = new AuditEventBatch
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = device.OrganizationId,
                    DeviceId = device.Id,
                    BatchId = request.BatchId,
                    EventCount = request.Events.Count,
                    ReceivedAtUtc = nowUtc,
                    AgentVersion = request.AgentVersion,
                    PolicyVersion = request.PolicyVersion
                };

                _db.AuditEventBatches.Add(batch);

                foreach (var incomingEvent in request.Events)
                {
                    var eventTypeName = ResolveEventTypeName(incomingEvent);

                    var metadataJson = incomingEvent.Metadata.HasValue
                        ? incomingEvent.Metadata.Value.GetRawText()
                        : null;

                    Guid? employeeId = null;

                    if (employeeBySid.TryGetValue(incomingEvent.UserSid, out var foundEmployeeId))
                    {
                        employeeId = foundEmployeeId;
                    }

                    var auditEvent = new AuditEvent
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = device.OrganizationId,

                        BatchRowId = batch.Id,

                        DeviceId = device.Id,
                        EmployeeId = employeeId,

                        UserSid = incomingEvent.UserSid,
                        Username = incomingEvent.Username,

                        ActionKey = incomingEvent.ActionKey,
                        EventTypeId = eventTypeMap[eventTypeName],
                        DecisionId = decisionMap[incomingEvent.Decision],
                        ReasonCodeId = reasonCodeMap[incomingEvent.ReasonCode],

                        PermissionGrantId = null,
                        PolicyVersion = request.PolicyVersion,

                        OccurredAtUtc = incomingEvent.OccurredAtUtc,
                        ReceivedAtUtc = nowUtc,

                        AgentVersion = request.AgentVersion,
                        CorrelationId = incomingEvent.CorrelationId,

                        MetadataJson = metadataJson
                    };

                    _db.AuditEvents.Add(auditEvent);
                }

                device.LastSeenAtUtc = nowUtc;
                device.AgentVersion = request.AgentVersion;
                device.CurrentPolicyVersion = request.PolicyVersion;
                device.UpdatedAtUtc = nowUtc;

                var policyState = await _db.DevicePolicyStates
                    .FirstOrDefaultAsync(x => x.DeviceId == device.Id, cancellationToken);

                if (policyState == null)
                {
                    policyState = new DevicePolicyState
                    {
                        DeviceId = device.Id,
                        OrganizationId = device.OrganizationId
                    };

                    _db.DevicePolicyStates.Add(policyState);
                }

                if (request.PolicyVersion > 0)
                {
                    policyState.LastAppliedPolicyVersion = request.PolicyVersion;
                    policyState.LastAppliedAtUtc = nowUtc;
                }

                credential.LastUsedAtUtc = nowUtc;

                await _db.SaveChangesAsync(cancellationToken);

                var result = new AgentAuditBatchResultDto
                {
                    BatchId = request.BatchId,
                    ReceivedEvents = request.Events.Count,
                    AlreadyProcessed = false
                };

                return ApiResponse<AgentAuditBatchResultDto>.SuccessResponse(
                    result,
                    "Audit events received successfully.",
                    "تم استلام الأحداث بنجاح");
            }
            catch (Exception)
            {
                return ApiResponse<AgentAuditBatchResultDto>.FailureResponse(
                    "Unexpected error occurred while receiving audit events.",
                    "حدث خطأ غير متوقع أثناء استلام الأحداث");
            }
        }

        private static string? ValidateRequestHeadersAndBody(
            string deviceKey,
            string agentSecret,
            AgentAuditBatchRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(deviceKey))
            {
                return "X-Device-Key header is required.";
            }

            if (string.IsNullOrWhiteSpace(agentSecret))
            {
                return "X-Agent-Secret header is required.";
            }

            if (request == null)
            {
                return "Request body is required.";
            }

            if (request.BatchId == Guid.Empty)
            {
                return "BatchId is required.";
            }

            if (string.IsNullOrWhiteSpace(request.AgentVersion))
            {
                return "AgentVersion is required.";
            }

            if (request.Events == null || request.Events.Count == 0)
            {
                return "At least one audit event is required.";
            }

            return null;
        }

        private static string? ValidateEvent(
            AgentAuditEventRequestDto incomingEvent,
            HashSet<string> validActionKeys,
            Dictionary<string, int> decisionMap,
            Dictionary<string, int> reasonCodeMap,
            Dictionary<string, int> eventTypeMap)
        {
            if (incomingEvent.CorrelationId == Guid.Empty)
            {
                return "CorrelationId is required.";
            }

            if (string.IsNullOrWhiteSpace(incomingEvent.UserSid))
            {
                return "UserSid is required.";
            }

            if (string.IsNullOrWhiteSpace(incomingEvent.Username))
            {
                return "Username is required.";
            }

            if (string.IsNullOrWhiteSpace(incomingEvent.ActionKey))
            {
                return "ActionKey is required.";
            }

            if (!validActionKeys.Contains(incomingEvent.ActionKey))
            {
                return $"ActionKey '{incomingEvent.ActionKey}' is not registered.";
            }

            if (string.IsNullOrWhiteSpace(incomingEvent.Decision))
            {
                return "Decision is required.";
            }

            if (!decisionMap.ContainsKey(incomingEvent.Decision))
            {
                return $"Decision '{incomingEvent.Decision}' is not registered.";
            }

            if (string.IsNullOrWhiteSpace(incomingEvent.ReasonCode))
            {
                return "ReasonCode is required.";
            }

            if (!reasonCodeMap.ContainsKey(incomingEvent.ReasonCode))
            {
                return $"ReasonCode '{incomingEvent.ReasonCode}' is not registered.";
            }

            var eventTypeName = ResolveEventTypeName(incomingEvent);

            if (!eventTypeMap.ContainsKey(eventTypeName))
            {
                return $"EventType '{eventTypeName}' is not registered.";
            }

            if (incomingEvent.OccurredAtUtc == default)
            {
                return "OccurredAtUtc is required.";
            }

            return null;
        }

        private static string ResolveEventTypeName(AgentAuditEventRequestDto incomingEvent)
        {
            if (!string.IsNullOrWhiteSpace(incomingEvent.EventType))
            {
                return incomingEvent.EventType;
            }

            return incomingEvent.Decision.Equals("Allow", StringComparison.OrdinalIgnoreCase)
                ? "ActionAllowed"
                : "ActionBlocked";
        }
    }
}
