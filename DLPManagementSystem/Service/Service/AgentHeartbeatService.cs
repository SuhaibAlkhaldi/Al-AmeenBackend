using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentHeartbeat;
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
            Guid organizationId,
            Guid deviceId,
            AgentHeartbeatRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.TenantId != organizationId || request.DeviceId != deviceId)
            {
                return ApiResponse<AgentHeartbeatResultDto>.FailureResponse(
                    "tenantId/deviceId do not match the authenticated device.",
                    "معرّف المؤسسة أو الجهاز لا يطابق الجهاز المصادق عليه");
            }

            var device = await _db.Devices
                .FirstOrDefaultAsync(x => x.Id == deviceId && x.OrganizationId == organizationId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<AgentHeartbeatResultDto>.FailureResponse(
                    "Device was not found.",
                    "الجهاز غير مسجل في النظام");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            device.LastSeenAtUtc = nowUtc;
            device.AgentVersion = request.AgentVersion;
            device.UpdatedAtUtc = nowUtc;

            if (!string.IsNullOrWhiteSpace(request.OsVersion))
            {
                device.OperatingSystem = request.OsVersion;
                device.OsVersion = request.OsVersion;
            }

            var latestPolicyVersion = await _db.PolicyVersions
                .Where(x => x.OrganizationId == organizationId)
                .OrderByDescending(x => x.VersionNumber)
                .Select(x => (long?)x.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;

            var policyRefreshRequired = latestPolicyVersion > request.LastAppliedPolicyVersion;

            device.CurrentPolicyVersion = request.LastAppliedPolicyVersion;

            await DevicePolicyStateUpsert.ApplyAsync(
                _db,
                device.Id,
                device.OrganizationId,
                policyState =>
                {
                    policyState.LastAppliedPolicyVersion = request.LastAppliedPolicyVersion;
                    policyState.LastAppliedAtUtc = nowUtc;
                },
                cancellationToken);

            var result = new AgentHeartbeatResultDto
            {
                ServerTimeUtc = nowUtc,
                PolicyRefreshRequired = policyRefreshRequired
            };

            return ApiResponse<AgentHeartbeatResultDto>.SuccessResponse(
                result,
                "Heartbeat received successfully.",
                "تم استلام نبضة الجهاز بنجاح");
        }
    }
}
