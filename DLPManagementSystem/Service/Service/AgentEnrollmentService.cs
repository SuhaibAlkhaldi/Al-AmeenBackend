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
        private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromDays(180);

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

                var deviceAlreadyExists = await _db.Devices
                    .AnyAsync(x => x.Id == request.DeviceId, cancellationToken);

                if (deviceAlreadyExists)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        $"Device '{request.DeviceId}' is already enrolled.",
                        "هذا الجهاز مسجل مسبقًا");
                }

                var device = new Device
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

                enrollmentToken.UsedCount += 1;

                await _db.SaveChangesAsync(cancellationToken);

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
