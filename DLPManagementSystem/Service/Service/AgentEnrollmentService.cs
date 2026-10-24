using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentEnrollment;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DLPManagementSystem.Service.Service
{
    public class AgentEnrollmentService : IAgentEnrollmentService
    {
        // Long-lived by design - manual revoke via the admin portal (DeviceService.DeleteDeviceAsync /
        // device revoke) is the real security control here, not time-based expiry.
        private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromDays(1825);

        private readonly DLPSystemContext _db;
        private readonly ILogger<AgentEnrollmentService> _logger;

        public AgentEnrollmentService(DLPSystemContext db, ILogger<AgentEnrollmentService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ApiResponse<AgentEnrollResponseDto>> Enroll(AgentEnrollRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var validationError = ValidateRequest(request);

                if (validationError != null)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        validationError,
                        "البيانات المرسلة غير صحيحة");
                }

                var nowUtc = DateTimeOffset.UtcNow;
                var codeHash = SecurityHashHelper.Sha256(request.EnrollmentCode);

                var enrollmentToken = await _db.AgentEnrollmentTokens
                    .FirstOrDefaultAsync(x =>
                        x.TokenHash == codeHash &&
                        x.RevokedAtUtc == null &&
                        x.ExpiresAtUtc > nowUtc,
                        cancellationToken);

                if (enrollmentToken == null)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        "Invalid or expired enrollment code.",
                        "رمز تسجيل الجهاز غير صحيح أو منتهي الصلاحية");
                }

                if (enrollmentToken.MaxUses > 0 && enrollmentToken.UsedCount >= enrollmentToken.MaxUses)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        "Enrollment code usage limit has been reached.",
                        "تم الوصول إلى الحد الأقصى لاستخدام رمز التسجيل");
                }

                if (request.TenantId != enrollmentToken.OrganizationId)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        "tenantId does not match the organization for this enrollment code.",
                        "معرّف المؤسسة لا يطابق رمز التسجيل");
                }

                var activeDeviceStatus = await _db.DeviceStatuses
                    .FirstOrDefaultAsync(x => x.Name == "Active", cancellationToken);

                if (activeDeviceStatus == null)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        "Active device status was not found.",
                        "حالة الجهاز Active غير موجودة في قاعدة البيانات");
                }

                var existingDevice = await _db.Devices
                    .Include(x => x.Status)
                    .FirstOrDefaultAsync(x => x.Id == request.DeviceId, cancellationToken);

                // A device that was properly uninstalled (see AgentEnrollmentService.Unenroll /
                // DeviceService.DeleteDeviceAsync) is left as "Retired", not deleted - so the exact same
                // DeviceId (persisted client-side in identity.json, which uninstall intentionally never
                // erases) can legitimately come back and re-enroll. Anything else with this same Id -
                // still Active/Disabled/Lost/PendingEnrollment, or Retired under a DIFFERENT organization
                // (Device.Id is a client-supplied GUID, not org-scoped at the DB level, so this guards
                // against silently reassigning another tenant's device history) - is a real conflict and
                // keeps today's rejection.
                var canReactivate = existingDevice != null
                    && existingDevice.OrganizationId == enrollmentToken.OrganizationId
                    && existingDevice.Status.Name == "Retired";

                if (existingDevice != null && !canReactivate)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        $"Device '{request.DeviceId}' is already enrolled.",
                        "هذا الجهاز مسجل مسبقًا");
                }

                Device device;
                if (existingDevice == null)
                {
                    device = new Device
                    {
                        Id = request.DeviceId,
                        OrganizationId = enrollmentToken.OrganizationId,
                        DeviceKey = request.DeviceId.ToString("D"),
                        MachineName = request.MachineName,
                        AgentVersion = request.AgentVersion,
                        StatusId = activeDeviceStatus.Id,
                        EnrolledAtUtc = nowUtc,
                        LastSeenAtUtc = nowUtc,
                        CurrentPolicyVersion = 0,
                        CreatedAtUtc = nowUtc,
                        UpdatedAtUtc = nowUtc
                    };

                    _db.Devices.Add(device);
                }
                else
                {
                    device = existingDevice;
                    device.MachineName = request.MachineName;
                    device.AgentVersion = request.AgentVersion;
                    device.StatusId = activeDeviceStatus.Id;
                    device.EnrolledAtUtc = nowUtc;
                    device.LastSeenAtUtc = nowUtc;
                    device.UpdatedAtUtc = nowUtc;
                }

                enrollmentToken.UsedCount += 1;

                await _db.SaveChangesAsync(cancellationToken);

                // Runs for both branches - a no-op for a brand-new device (a fresh GUID can't already
                // have credentials), and the actual point for a reactivated one (revokes whatever
                // credential it held before it was retired).
                var previousCredentials = await _db.DeviceCredentials
                    .Where(x => x.DeviceId == device.Id && x.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);

                foreach (var previousCredential in previousCredentials)
                {
                    previousCredential.RevokedAtUtc = nowUtc;
                }

                var accessToken = SecurityHashHelper.GenerateSecret();
                var accessTokenHash = SecurityHashHelper.Sha256(accessToken);
                var expiresAtUtc = nowUtc.Add(AccessTokenLifetime);

                var credential = new DeviceCredential
                {
                    Id = Guid.NewGuid(),
                    DeviceId = device.Id,
                    SecretHash = accessTokenHash,
                    CreatedAtUtc = nowUtc,
                    LastUsedAtUtc = null,
                    RevokedAtUtc = null,
                    RotationDueAtUtc = expiresAtUtc
                };

                _db.DeviceCredentials.Add(credential);

                await _db.SaveChangesAsync(cancellationToken);

                var response = new AgentEnrollResponseDto
                {
                    AccessToken = accessToken,
                    ExpiresAtUtc = expiresAtUtc
                };

                return ApiResponse<AgentEnrollResponseDto>.SuccessResponse(
                    response,
                    "Agent enrolled successfully.",
                    "تم تسجيل الجهاز بنجاح");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Unexpected error while enrolling device {DeviceId} for organization {OrganizationId}.",
                    request?.DeviceId, request?.TenantId);
                return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                    "Unexpected error occurred while enrolling agent.",
                    "حدث خطأ غير متوقع أثناء تسجيل الجهاز");
            }
        }

        public async Task<ApiResponse<AgentUnenrollResponseDto>> Unenroll(
            Guid organizationId, Guid deviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var device = await _db.Devices
                    .Include(x => x.Status)
                    .FirstOrDefaultAsync(x => x.Id == deviceId && x.OrganizationId == organizationId, cancellationToken);

                if (device == null)
                {
                    return ApiResponse<AgentUnenrollResponseDto>.FailureResponse(
                        "Device was not found.",
                        "الجهاز غير موجود");
                }

                // Idempotent - uninstall may be re-run (e.g. the first attempt reached the backend but
                // the agent never saw the response before the process was torn down), so an
                // already-Retired device is a success, not an error.
                if (!device.Status.Name.Equals("Retired", StringComparison.OrdinalIgnoreCase))
                {
                    var retiredStatus = await _db.DeviceStatuses
                        .FirstOrDefaultAsync(x => x.Name == "Retired", cancellationToken);

                    if (retiredStatus == null)
                    {
                        return ApiResponse<AgentUnenrollResponseDto>.FailureResponse(
                            "Retired device status is not configured.",
                            "حالة الإيقاف غير مهيأة");
                    }

                    var nowUtc = DateTimeOffset.UtcNow;
                    device.StatusId = retiredStatus.Id;
                    device.UpdatedAtUtc = nowUtc;

                    var activeCredentials = await _db.DeviceCredentials
                        .Where(x => x.DeviceId == device.Id && x.RevokedAtUtc == null)
                        .ToListAsync(cancellationToken);

                    foreach (var activeCredential in activeCredentials)
                    {
                        activeCredential.RevokedAtUtc = nowUtc;
                    }

                    await _db.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Device {DeviceId} for organization {OrganizationId} was unenrolled (retired) by the agent.",
                        deviceId, organizationId);
                }

                return ApiResponse<AgentUnenrollResponseDto>.SuccessResponse(
                    new AgentUnenrollResponseDto { Unenrolled = true },
                    "Device unenrolled successfully.",
                    "تم إلغاء تسجيل الجهاز بنجاح");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Unexpected error while unenrolling device {DeviceId} for organization {OrganizationId}.",
                    deviceId, organizationId);
                return ApiResponse<AgentUnenrollResponseDto>.FailureResponse(
                    "Unexpected error occurred while unenrolling agent.",
                    "حدث خطأ غير متوقع أثناء إلغاء تسجيل الجهاز");
            }
        }

        private static string? ValidateRequest(AgentEnrollRequestDto request)
        {
            if (request == null)
            {
                return "Request body is required.";
            }

            if (request.TenantId == Guid.Empty)
            {
                return "tenantId is required.";
            }

            if (request.DeviceId == Guid.Empty)
            {
                return "deviceId is required.";
            }

            if (string.IsNullOrWhiteSpace(request.MachineName))
            {
                return "machineName is required.";
            }

            if (string.IsNullOrWhiteSpace(request.AgentVersion))
            {
                return "agentVersion is required.";
            }

            if (string.IsNullOrWhiteSpace(request.EnrollmentCode))
            {
                return "enrollmentCode is required.";
            }

            return null;
        }
    }
}
