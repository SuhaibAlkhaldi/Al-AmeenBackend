using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Users;

namespace DLPManagementSystem.Service.Interface
{
    public interface IUserService
    {
        Task<ApiResponse<PagedResultDto<UserListItemDto>>> GetUsersAsync(
            Guid organizationId,
            string? search,
            int? roleId,
            int? statusId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);

        Task<ApiResponse<UserDetailDto>> CreateUserAsync(Guid organizationId, CreateUserDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid organizationId, Guid id, UpdateUserDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> ResetPasswordAsync(Guid organizationId, Guid id, ResetPasswordDto request, CancellationToken cancellationToken = default);

        Task<ApiResponse<bool>> DeleteUserAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default);
    }
}
