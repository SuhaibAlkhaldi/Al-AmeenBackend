using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentEnrollment;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class AgentEnrollmentService : IAgentEnrollmentService
    {
        private readonly DLPSystemContext _db;

        public AgentEnrollmentService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<AgentEnrollResponseDto>> Enroll(AgentEnrollRequestDto request,CancellationToken cancellationToken = default)
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

                var nowUtc = DateTime.UtcNow;
                var tokenHash = SecurityHashHelper.Sha256(request.EnrollmentToken);

                var enrollmentToken = await _db.AgentEnrollmentTokens
                    .FirstOrDefaultAsync(x =>
                        x.TokenHash == tokenHash &&
                        x.RevokedAtUtc == null &&
                        x.ExpiresAtUtc > nowUtc,
                        cancellationToken);

                if (enrollmentToken == null)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        "Invalid or expired enrollment token.",
                        "رمز تسجيل الجهاز غير صحيح أو منتهي الصلاحية");
                }

                if (enrollmentToken.MaxUses > 0 &&enrollmentToken.UsedCount >= enrollmentToken.MaxUses)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        "Enrollment token usage limit has been reached.",
                        "تم الوصول إلى الحد الأقصى لاستخدام رمز التسجيل");
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
                    .AnyAsync(x =>
                        x.OrganizationId == enrollmentToken.OrganizationId &&
                        (
                            x.MachineName == request.MachineName ||
                            (!string.IsNullOrWhiteSpace(request.MachineSid) &&
                             x.MachineSid == request.MachineSid)
                        ),
                        cancellationToken);

                if (deviceAlreadyExists)
                {
                    return ApiResponse<AgentEnrollResponseDto>.FailureResponse(
                        $"Device '{request.MachineName}' is already enrolled.",
                        "هذا الجهاز مسجل مسبقًا");
                }

                var deviceKey = $"dev-{Guid.NewGuid():N}";
                var agentSecret = SecurityHashHelper.GenerateSecret();
                var agentSecretHash = SecurityHashHelper.Sha256(agentSecret);

                var device = new Device
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = enrollmentToken.OrganizationId,
                    DeviceKey = deviceKey,
                    MachineName = request.MachineName,
                    MachineSid = request.MachineSid,
                    OperatingSystem = request.OperatingSystem,
                    OsVersion = request.OsVersion,
                    SerialNumber = request.SerialNumber,
                    MacAddress = request.MacAddress,
                    AgentVersion = request.AgentVersion,
                    StatusId = activeDeviceStatus.Id,
                    EnrolledAtUtc = nowUtc,
                    LastSeenAtUtc = nowUtc,
                    CurrentPolicyVersion = 1,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };

                _db.Devices.Add(device);

                enrollmentToken.UsedCount += 1;

                await _db.SaveChangesAsync(cancellationToken);

                var credential = new DeviceCredential
                {
                    Id = Guid.NewGuid(),
                    DeviceId = device.Id,
                    SecretHash = agentSecretHash,
                    CreatedAtUtc = nowUtc,
                    LastUsedAtUtc = null,
                    RevokedAtUtc = null,
                    RotationDueAtUtc = nowUtc.AddMonths(6)
                };

                _db.DeviceCredentials.Add(credential);

                await _db.SaveChangesAsync(cancellationToken);

                var response = new AgentEnrollResponseDto
                {
                    DeviceKey = device.DeviceKey,
                    AgentSecret = agentSecret,
                    EnrolledAtUtc = nowUtc
                };

                return ApiResponse<AgentEnrollResponseDto>.SuccessResponse(
                    response,
                    "Agent enrolled successfully.",
                    "تم تسجيل الجهاز بنجاح");
            }
            catch (Exception)
            {
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

            if (string.IsNullOrWhiteSpace(request.EnrollmentToken))
            {
                return "EnrollmentToken is required.";
            }

            if (string.IsNullOrWhiteSpace(request.MachineName))
            {
                return "MachineName is required.";
            }

            if (string.IsNullOrWhiteSpace(request.OperatingSystem))
            {
                return "OperatingSystem is required.";
            }

            if (string.IsNullOrWhiteSpace(request.AgentVersion))
            {
                return "AgentVersion is required.";
            }

            return null;
        }
    }
}
