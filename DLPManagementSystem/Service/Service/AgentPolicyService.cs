using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentPolicy;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class AgentPolicyService : IAgentPolicyService
    {
        private readonly DLPSystemContext _db;

        public AgentPolicyService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<AgentPolicyResultDto>> GetPolicyAsync(
            string deviceKey,
            string agentSecret,
            Guid tenantId,
            Guid deviceId,
            long currentVersion,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceKey))
            {
                return ApiResponse<AgentPolicyResultDto>.FailureResponse(
                    "X-Device-Key header is required.",
                    "مفتاح الجهاز مطلوب");
            }

            if (string.IsNullOrWhiteSpace(agentSecret))
            {
                return ApiResponse<AgentPolicyResultDto>.FailureResponse(
                    "X-Agent-Secret header is required.",
                    "سر الجهاز مطلوب");
            }

            var nowUtc = DateTimeOffset.UtcNow;
            var agentSecretHash = SecurityHashHelper.Sha256(agentSecret);

            var device = await _db.Devices
                .FirstOrDefaultAsync(x => x.DeviceKey == deviceKey, cancellationToken);

            if (device == null)
            {
                return ApiResponse<AgentPolicyResultDto>.FailureResponse(
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
                return ApiResponse<AgentPolicyResultDto>.FailureResponse(
                    "Invalid device credentials.",
                    "بيانات اعتماد الجهاز غير صحيحة");
            }

            device.LastSeenAtUtc = nowUtc;
            device.UpdatedAtUtc = nowUtc;
            credential.LastUsedAtUtc = nowUtc;

            var latestPolicyVersion = await _db.PolicyVersions
                .Where(x => x.OrganizationId == device.OrganizationId)
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestPolicyVersion == null || latestPolicyVersion.VersionNumber <= currentVersion)
            {
                await _db.SaveChangesAsync(cancellationToken);

                return ApiResponse<AgentPolicyResultDto>.SuccessResponse(
                    new AgentPolicyResultDto
                    {
                        HasUpdate = false,
                        Snapshot = null
                    },
                    "No policy update is available.",
                    "لا يوجد تحديث جديد للسياسة");
            }

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

            policyState.LastFetchedPolicyVersion = latestPolicyVersion.VersionNumber;
            policyState.LastFetchedAtUtc = nowUtc;

            var snapshot = BuildPolicySnapshot(
                 latestPolicyVersion.Id,
                 latestPolicyVersion.VersionNumber,
                 tenantId,
                 deviceId,
                 nowUtc);

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<AgentPolicyResultDto>.SuccessResponse(
                new AgentPolicyResultDto
                {
                    HasUpdate = true,
                    Snapshot = snapshot
                },
                "Policy update is available.",
                "يوجد تحديث جديد للسياسة");
        }

        private static AgentPolicySnapshotDto BuildPolicySnapshot(
            Guid policyId,
            long versionNumber,
            Guid organizationId,
            Guid deviceId,
            DateTimeOffset nowUtc)
        {
            return new AgentPolicySnapshotDto
            {
                PolicyId = policyId,
                Version = versionNumber,
                TenantId = organizationId,
                DeviceId = deviceId,
                IssuedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddDays(7),
                SignatureAlgorithm = "DEVELOPMENT",
                SignatureBase64 = "DEVELOPMENT-UNSIGNED",
                Policy = new AgentDlpPolicyDto
                {
                    PolicyVersion = $"central-{versionNumber}",
                    Enabled = true,
                    Runtime = new AgentRuntimePolicyDto
                    {
                        Mode = "Development",
                        PersistentProtection = false,
                        PolicyReapplySeconds = 15,
                        KeepSessionAgentRunning = true,
                        SessionAgentPollSeconds = 5
                    },
                    Backend = new AgentBackendPolicyDto
                    {
                        Enabled = true,
                        TenantId = organizationId,
                        Mode = "Development",
                        BaseUrl = "https://localhost:7008",
                        RequestTimeoutSeconds = 15,
                        AuditBatchSize = 100,
                        AuditSyncSeconds = 3,
                        PolicySyncSeconds = 10,
                        HeartbeatSeconds = 15,
                        AllowUnsignedDevelopmentPolicy = true,
                        PolicySigningPublicKeyPem = "",
                        AuthenticationMode = "DeviceBearerToken",
                        CredentialName = "agent-access-token"
                    },
                    Permissions = new AgentPermissionPolicyDto
                    {
                        DefaultPermissions = BuildDefaultPermissions(),
                        Grants = new List<object>()
                    },
                    SensitiveRules = BuildDefaultSensitiveRules()
                }
            };
        }
        private static List<AgentSensitiveRuleDto> BuildDefaultSensitiveRules()
        {
            return new List<AgentSensitiveRuleDto>
    {
        new AgentSensitiveRuleDto
        {
            Id = "word-confidential",
            Name = "Confidential keyword",
            Type = "Keyword",
            Value = "confidential",
            Pattern = "",
            Enabled = true,
            CaseSensitive = false,
            Normalize = false,
            DetectFragments = false,
            BlockIndividualFragments = false,
            MinimumBlockedFragmentLength = 3
        },
        new AgentSensitiveRuleDto
        {
            Id = "any-email-address",
            Name = "Any email address",
            Type = "AnyEmail",
            Value = "",
            Pattern = "",
            Enabled = true,
            CaseSensitive = false,
            Normalize = false,
            DetectFragments = true,
            BlockIndividualFragments = false,
            MinimumBlockedFragmentLength = 3
        },
        new AgentSensitiveRuleDto
        {
            Id = "iban-regex",
            Name = "IBAN pattern",
            Type = "Regex",
            Value = "",
            Pattern = "\\b[A-Z]{2}\\d{2}[A-Z0-9]{11,30}\\b",
            Enabled = true,
            CaseSensitive = false,
            Normalize = false,
            DetectFragments = false,
            BlockIndividualFragments = false,
            MinimumBlockedFragmentLength = 3
        }
    };
        }
        private static Dictionary<string, bool> BuildDefaultPermissions()
        {
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["screen.capture"] = false,
                ["screen.recording"] = false,

                ["clipboard.copy-sensitive"] = false,

                ["browser.download"] = false,
                ["browser.upload"] = false,
                ["browser.drag-drop"] = false,
                ["browser.file-paste"] = false,
                ["browser.image-paste"] = false,

                ["software.install"] = false,

                ["usb.device-connect"] = false,
                ["usb.storage"] = false,
                ["usb.mobile-device"] = false,

                ["file.encrypt"] = true,
                ["file.decrypt"] = true,

                ["policy.apply"] = true
            };
        }
    }
}
