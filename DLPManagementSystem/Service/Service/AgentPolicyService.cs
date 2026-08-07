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
        private readonly IPolicySigningService _policySigningService;

        public AgentPolicyService(DLPSystemContext db, IPolicySigningService policySigningService)
        {
            _db = db;
            _policySigningService = policySigningService;
        }

        public async Task<ApiResponse<AgentPolicyResultDto>> GetPolicyAsync(
            Guid organizationId,
            Guid deviceId,
            long currentVersion,
            string? userSid,
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
            var grants = await BuildGrantsForDeviceAsync(organizationId, device.Id, userSid, nowUtc, cancellationToken);
            var watermarkEnabled = BuildEffectiveWatermarkEnabled(grants);

            var snapshot = BuildPolicySnapshot(
                 latestPolicyVersion.Id,
                 latestPolicyVersion.VersionNumber,
                 organizationId,
                 deviceId,
                 nowUtc,
                 defaultPermissions,
                 grants,
                 watermarkEnabled);

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

        // The device's currently-active employee's active grants, re-keyed to a DeviceId-scoped
        // grant (the agent already knows how to match DeviceId — it has no concept of "Employee").
        // Includes grants targeted at this specific device as well as employee-wide ones (TargetDeviceId
        // null). Only RuntimeStatus == Active grants are sent; Pending/Expired/Revoked stay server-side.
        //
        // Shared-device support: a device can have more than one active DeviceUserAssignment (a shared
        // workstation authorized for several employees). "Currently-active employee" is resolved as:
        //   - Zero active assignments: no employee (unchanged from before shared-device support).
        //   - Exactly one active assignment: that employee, unconditionally — deliberately NOT gated on
        //     userSid matching anything, so every existing single-assignment device keeps working
        //     exactly as it does today even though EmployeeWindowsIdentities is not populated for it
        //     (this is the compatibility guarantee this feature was built under).
        //   - More than one active assignment: resolved via userSid (the Agent's current interactive
        //     console user, sent as an unsigned query parameter — see AgentPolicyController) matched
        //     against EmployeeWindowsIdentities, restricted to only the employees officially assigned to
        //     THIS device. No match (unrecognized SID, or no SID sent) falls through to no employee —
        //     same "unknown subject -> GlobalDefaultDeny/Allow" behavior as always, never "pick anyone."
        private async Task<List<AgentPermissionGrantDto>> BuildGrantsForDeviceAsync(
            Guid organizationId,
            Guid deviceId,
            string? userSid,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
        {
            var activeAssignedEmployeeIds = await _db.DeviceUserAssignments
                .AsNoTracking()
                .Where(x => x.DeviceId == deviceId && x.UnassignedAtUtc == null)
                .Select(x => x.EmployeeId)
                .ToListAsync(cancellationToken);

            Guid? resolvedEmployeeId = activeAssignedEmployeeIds.Count switch
            {
                0 => null,
                1 => activeAssignedEmployeeIds[0],
                _ => await ResolveEmployeeBySidAsync(organizationId, activeAssignedEmployeeIds, userSid, cancellationToken)
            };

            if (resolvedEmployeeId == null)
            {
                return new List<AgentPermissionGrantDto>();
            }

            var employeeIdString = resolvedEmployeeId.Value.ToString();

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
                    x.RevokedAtUtc,
                    x.FileHash,
                    x.ClassificationTier
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
                    RevokedBy = "",
                    FileHash = g.FileHash,
                    ClassificationTier = g.ClassificationTier
                })
                .ToList();
        }

        // Only called when a device has more than one active assignment (see BuildGrantsForDeviceAsync).
        // Deliberately restricts the EmployeeWindowsIdentities lookup to candidateEmployeeIds (the
        // employees actually assigned to THIS device) rather than searching org-wide by SID - the
        // official per-device assignment stays the authorization boundary; a SID that happens to be
        // registered for some other employee elsewhere in the organization must never grant that
        // employee's policy on a device they were never assigned to.
        private async Task<Guid?> ResolveEmployeeBySidAsync(
            Guid organizationId,
            List<Guid> candidateEmployeeIds,
            string? userSid,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userSid))
            {
                return null;
            }

            return await _db.EmployeeWindowsIdentities
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId
                    && candidateEmployeeIds.Contains(x.EmployeeId)
                    && x.UserSid == userSid
                    && x.RevokedAtUtc == null)
                .Select(x => (Guid?)x.EmployeeId)
                .FirstOrDefaultAsync(cancellationToken);
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

        private AgentPolicySnapshotDto BuildPolicySnapshot(
            Guid policyId,
            long versionNumber,
            Guid organizationId,
            Guid deviceId,
            DateTimeOffset nowUtc,
            Dictionary<string, bool> defaultPermissions,
            List<AgentPermissionGrantDto> grants,
            bool watermarkEnabled)
        {
            // Runtime/Backend below are agent-local-only concerns (see PolicyStore.
            // PreserveLocalOnlySections on the agent side) - left at their own honest defaults rather
            // than fabricated values this backend has no authority over. TenantId is the one real,
            // backend-known fact worth actually sending in Backend; everything else there is just
            // "what this section defaults to," present only so the signed payload's shape matches
            // what the agent reconstructs.
            //
            // Runtime.Mode is the one exception - it MUST be derived from whether signing is actually
            // going to produce a real signature (_policySigningService.HasRealKey), never hardcoded
            // independently. PolicySnapshotValidator's unsigned-dev bypass on the agent side requires
            // BOTH SignatureBase64=="DEVELOPMENT-UNSIGNED" AND Runtime.Mode=="Development" to be true
            // together - if this ever hardcodes "Production" again while a real key isn't configured,
            // every Development-mode agent's policy sync silently breaks again exactly as before.
            var runtimeMode = _policySigningService.HasRealKey ? "Production" : "Development";

            var snapshot = new AgentPolicySnapshotDto
            {
                PolicyId = policyId,
                Version = versionNumber,
                TenantId = organizationId,
                DeviceId = deviceId,
                IssuedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddDays(7),
                Policy = new AgentDlpPolicyDto
                {
                    PolicyVersion = $"central-{versionNumber}",
                    Enabled = true,
                    Runtime = new AgentRuntimePolicyDto { Mode = runtimeMode },
                    Backend = new AgentBackendPolicyDto
                    {
                        TenantId = organizationId
                    },
                    Watermark = new AgentWatermarkPolicyDto { Enabled = watermarkEnabled },
                    Permissions = new AgentPermissionPolicyDto
                    {
                        DefaultPermissions = defaultPermissions,
                        Grants = grants
                    },
                    SensitiveRules = BuildDefaultSensitiveRules()
                }
            };

            var (algorithm, signatureBase64) = _policySigningService.Sign(snapshot);
            snapshot.SignatureAlgorithm = algorithm;
            snapshot.SignatureBase64 = signatureBase64;
            return snapshot;
        }

        // Watermark is protected by default. An effective Allow for this exception removes it;
        // an effective Deny, expiry, revocation, or no grant leaves it enabled.
        private static bool BuildEffectiveWatermarkEnabled(List<AgentPermissionGrantDto> grants)
        {
            var isExempt = grants
                .Where(value => value.ActionKey == PermissionActionKeys.WatermarkDisable)
                .OrderByDescending(value => value.Priority)
                .ThenByDescending(value => value.CreatedAtUtc)
                .Select(value => (bool?)value.Allowed)
                .FirstOrDefault() is true;

            return !isExempt;
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
