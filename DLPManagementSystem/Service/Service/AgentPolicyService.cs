using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentPolicy;
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
            Guid organizationId,
            Guid deviceId,
            long currentVersion,
            CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTimeOffset.UtcNow;

            var device = await _db.Devices
                .FirstOrDefaultAsync(x => x.Id == deviceId && x.OrganizationId == organizationId, cancellationToken);

            if (device == null)
            {
                return ApiResponse<AgentPolicyResultDto>.FailureResponse(
                    "Device was not found.",
                    "الجهاز غير مسجل في النظام");
            }

            device.LastSeenAtUtc = nowUtc;
            device.UpdatedAtUtc = nowUtc;

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

            var defaultPermissions = await BuildDefaultPermissionsAsync(cancellationToken);
            var grants = await BuildGrantsForDeviceAsync(organizationId, device.Id, nowUtc, cancellationToken);

            var snapshot = BuildPolicySnapshot(
                 latestPolicyVersion.Id,
                 latestPolicyVersion.VersionNumber,
                 organizationId,
                 deviceId,
                 nowUtc,
                 defaultPermissions,
                 grants);

            await DevicePolicyStateUpsert.ApplyAsync(
                _db,
                device.Id,
                device.OrganizationId,
                policyState =>
                {
                    policyState.LastFetchedPolicyVersion = latestPolicyVersion.VersionNumber;
                    policyState.LastFetchedAtUtc = nowUtc;
                },
                cancellationToken);

            return ApiResponse<AgentPolicyResultDto>.SuccessResponse(
                new AgentPolicyResultDto
                {
                    HasUpdate = true,
                    Snapshot = snapshot
                },
                "Policy update is available.",
                "يوجد تحديث جديد للسياسة");
        }

        // The device's currently-assigned employee's active grants, re-keyed to a DeviceId-scoped
        // grant (the agent already knows how to match DeviceId — it has no concept of "Employee").
        // Includes grants targeted at this specific device as well as employee-wide ones (TargetDeviceId
        // null). Only RuntimeStatus == Active grants are sent; Pending/Expired/Revoked stay server-side.
        private async Task<List<AgentPermissionGrantDto>> BuildGrantsForDeviceAsync(
            Guid organizationId,
            Guid deviceId,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
        {
            var assignedEmployeeId = await _db.DeviceUserAssignments
                .AsNoTracking()
                .Where(x => x.DeviceId == deviceId && x.UnassignedAtUtc == null)
                .Select(x => (Guid?)x.EmployeeId)
                .FirstOrDefaultAsync(cancellationToken);

            if (assignedEmployeeId == null)
            {
                return new List<AgentPermissionGrantDto>();
            }

            var employeeIdString = assignedEmployeeId.Value.ToString();

            var candidates = await _db.PermissionGrants
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId
                    && x.SubjectType.Name == "Employee"
                    && x.SubjectId == employeeIdString
                    && (x.TargetDeviceId == null || x.TargetDeviceId == deviceId))
                .Select(x => new
                {
                    x.Id,
                    x.ActionKey,
                    DecisionName = x.Decision.Name,
                    GrantTypeName = x.GrantType.Name,
                    x.Priority,
                    x.StartsAtUtc,
                    x.ExpiresAtUtc,
                    x.Reason,
                    GrantedByName = x.GrantedByUser.FullName,
                    x.CreatedAtUtc,
                    x.RevokedAtUtc
                })
                .ToListAsync(cancellationToken);

            return candidates
                .Where(g => PermissionGrantRuntimeStatus.Compute(g.RevokedAtUtc, g.ExpiresAtUtc, g.StartsAtUtc, nowUtc) == "Active")
                .Select(g => new AgentPermissionGrantDto
                {
                    GrantId = g.Id,
                    ActionKey = g.ActionKey,
                    Allowed = g.DecisionName == "Allow",
                    SubjectType = "DeviceId",
                    SubjectId = deviceId.ToString(),
                    Source = g.GrantTypeName == "Temporary" ? "TemporaryGrant" : "PermanentPolicy",
                    Priority = g.Priority,
                    StartsAtUtc = g.StartsAtUtc,
                    ExpiresAtUtc = g.ExpiresAtUtc,
                    Reason = g.Reason,
                    GrantedBy = g.GrantedByName,
                    CreatedAtUtc = g.CreatedAtUtc,
                    RevokedAtUtc = null,
                    RevokedBy = null
                })
                .ToList();
        }

        // The real, admin-manageable per-action default (Allow/Deny) from PermissionActions —
        // replaces what used to be a hardcoded dictionary disconnected from this table.
        private async Task<Dictionary<string, bool>> BuildDefaultPermissionsAsync(CancellationToken cancellationToken)
        {
            var rows = await _db.PermissionActions
                .AsNoTracking()
                .Where(x => x.IsEnabled)
                .Select(x => new { x.Key, DecisionName = x.DefaultDecision.Name })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(
                x => x.Key,
                x => x.DecisionName == "Allow",
                StringComparer.OrdinalIgnoreCase);
        }

        private static AgentPolicySnapshotDto BuildPolicySnapshot(
            Guid policyId,
            long versionNumber,
            Guid organizationId,
            Guid deviceId,
            DateTimeOffset nowUtc,
            Dictionary<string, bool> defaultPermissions,
            List<AgentPermissionGrantDto> grants)
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
                        DefaultPermissions = defaultPermissions,
                        Grants = grants
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
    }
}
