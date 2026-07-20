using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentHeartbeat;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class AgentHeartbeatService : IAgentHeartbeatService
    {
        private readonly DLPSystemContext _db;

        public AgentHeartbeatService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<AgentHeartbeatResultDto>> ReceiveHeartbeatAsync(
            string deviceKey,
            string agentSecret,
            string? agentVersion,
            AgentHeartbeatRequestDto? request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceKey))
            {
                return ApiResponse<AgentHeartbeatResultDto>.FailureResponse(
                    "X-Device-Key header is required.",
                    "مفتاح الجهاز مطلوب");
            }

            if (string.IsNullOrWhiteSpace(agentSecret))
            {
                return ApiResponse<AgentHeartbeatResultDto>.FailureResponse(
                    "X-Agent-Secret header is required.",
                    "سر الجهاز مطلوب");
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var agentSecretHash = SecurityHashHelper.Sha256(agentSecret);

            var device = await _db.Devices
                .FirstOrDefaultAsync(x => x.DeviceKey == deviceKey, cancellationToken);

            if (device == null)
            {
                return ApiResponse<AgentHeartbeatResultDto>.FailureResponse(
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
                return ApiResponse<AgentHeartbeatResultDto>.FailureResponse(
                    "Invalid device credentials.",
                    "بيانات اعتماد الجهاز غير صحيحة");
            }

            var reportedPolicyVersion =
                request?.PolicyVersion ??
                request?.CurrentPolicyVersion;

            device.LastSeenAtUtc = nowUtc;
            device.UpdatedAtUtc = nowUtc;

            if (!string.IsNullOrWhiteSpace(agentVersion))
            {
                device.AgentVersion = agentVersion;
            }

            if (reportedPolicyVersion.HasValue && reportedPolicyVersion.Value > 0)
            {
                device.CurrentPolicyVersion = reportedPolicyVersion.Value;

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

                policyState.LastAppliedPolicyVersion = reportedPolicyVersion.Value;
                policyState.LastAppliedAtUtc = nowUtc;

                if (!string.IsNullOrWhiteSpace(request?.PolicyHash))
                {
                    policyState.LastPolicyHash = request.PolicyHash;
                }
            }

            credential.LastUsedAtUtc = nowUtc;

            await _db.SaveChangesAsync(cancellationToken);

            var result = new AgentHeartbeatResultDto
            {
                DeviceId = device.Id,
                LastSeenAtUtc = nowUtc,
                AgentVersion = device.AgentVersion,
                CurrentPolicyVersion = device.CurrentPolicyVersion
            };

            return ApiResponse<AgentHeartbeatResultDto>.SuccessResponse(
                result,
                "Heartbeat received successfully.",
                "تم استلام نبضة الجهاز بنجاح");
        }
    }
}
