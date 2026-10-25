using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Permissions.Contracts;
using DLPManagementSystem.Models;

namespace DLPManagementSystem.Service.Interface
{
    /// <summary>
    /// Result of the shared grant-construction step used by both the request-approval path and the
    /// admin direct-grant path, so they can never drift into inconsistent validation/behavior.
    /// </summary>
    public sealed class GrantBuildResult
    {
        public PermissionGrant? Grant { get; init; }
        public string? ErrorMessageEn { get; init; }
        public string? ErrorMessageAr { get; init; }

        public bool Success => Grant != null;

        public static GrantBuildResult Ok(PermissionGrant grant) => new() { Grant = grant };

        public static GrantBuildResult Failure(string messageEn, string messageAr) =>
            new() { ErrorMessageEn = messageEn, ErrorMessageAr = messageAr };
    }

    /// <summary>
    /// Result of resolving a grant's subject (Employee/Device/Organization) from a caller-supplied
    /// SubjectType + SubjectId, shared by the admin direct-grant path and the request-approval path's
    /// "approve with a different scope" override, so both validate/resolve identically.
    /// </summary>
    public sealed class GrantSubjectResolution
    {
        public int SubjectTypeId { get; init; }
        public string SubjectId { get; init; } = string.Empty;
        public Guid? TargetDeviceId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string? ErrorMessageEn { get; init; }
        public string? ErrorMessageAr { get; init; }

        public bool Success => ErrorMessageEn == null;

        public static GrantSubjectResolution Ok(int subjectTypeId, string subjectId, Guid? targetDeviceId, string displayName) =>
            new() { SubjectTypeId = subjectTypeId, SubjectId = subjectId, TargetDeviceId = targetDeviceId, DisplayName = displayName };

        public static GrantSubjectResolution Failure(string messageEn, string messageAr) =>
            new() { ErrorMessageEn = messageEn, ErrorMessageAr = messageAr };
    }

    public interface IPermissionGrantService
    {
        Task<ApiResponse<PagedResultDto<PermissionGrantDto>>> GetGrantsAsync(
            Guid organizationId,
            Guid callerUserId,
            int callerUserTypeId,
            string? subjectId,
            string? actionKey,
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> RevokeAsync(
            Guid organizationId,
            Guid id,
            Guid revokedByUserId,
            RevokePermissionGrantDto request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<RevokeAllGrantsResultDto>> RevokeAllAsync(
            Guid organizationId,
            Guid revokedByUserId,
            RevokeAllGrantsDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates and constructs a <see cref="PermissionGrant"/> (added to the context but not yet saved),
        /// shared by both the request-approval flow and the admin direct-grant flow. Callers are responsible
        /// for any additional mutations, the policy version bump, and calling SaveChangesAsync.
        /// </summary>
        Task<GrantBuildResult> BuildGrantAsync(
            Guid organizationId,
            string actionKey,
            int decisionId,
            int subjectTypeId,
            string subjectId,
            Guid? targetDeviceId,
            string grantTypeName,
            DateTimeOffset? requestedStartsAtUtc,
            DateTimeOffset? requestedExpiresAtUtc,
            string reason,
            Guid grantedByUserId,
            Guid? sourcePermissionRequestId,
            CancellationToken cancellationToken = default,
            string? fileHash = null,
            string? classificationTier = null);

        Task<ApiResponse<PermissionGrantDto>> CreateDirectGrantAsync(
            Guid organizationId,
            Guid grantedByUserId,
            CreatePermissionGrantDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates subjectTypeName ("Employee"/"Device"/"Organization") + subjectId and resolves the
        /// concrete SubjectTypeId/SubjectId/TargetDeviceId/display-name a grant for it needs. Shared by
        /// CreateDirectGrantAsync and PermissionRequestService.ApproveAsync's scope-override path.
        /// </summary>
        Task<GrantSubjectResolution> ResolveSubjectAsync(
            Guid organizationId,
            string subjectTypeName,
            string? subjectId,
            CancellationToken cancellationToken = default);
    }
}
