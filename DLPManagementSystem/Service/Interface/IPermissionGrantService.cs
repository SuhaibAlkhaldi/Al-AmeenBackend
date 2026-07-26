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
    }
}
