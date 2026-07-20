using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.EnrollmentTokens;

namespace DLPManagementSystem.Service.Interface
{
    public interface IEnrollmentTokenService
    {
        Task<ApiResponse<PagedResultDto<EnrollmentTokenDto>>> GetTokensAsync(
            Guid organizationId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<EnrollmentTokenCreatedDto>> CreateTokenAsync(
            Guid organizationId,
            Guid createdByUserId,
            CreateEnrollmentTokenDto request,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<object?>> RevokeTokenAsync(
            Guid organizationId,
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
