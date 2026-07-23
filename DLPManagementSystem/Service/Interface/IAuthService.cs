using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Auth;

namespace DLPManagementSystem.Service.Interface
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<AuthUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default);
    }
}
