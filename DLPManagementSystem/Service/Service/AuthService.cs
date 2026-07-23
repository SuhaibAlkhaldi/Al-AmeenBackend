using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Auth;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DLPManagementSystem.Service.Service
{
    public class AuthService : IAuthService
    {
        private readonly DLPSystemContext _db;
        private readonly IConfiguration _configuration;

        public AuthService(DLPSystemContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .Include(x => x.Role)
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

            if (user == null || user.PasswordHash != SecurityHashHelper.Sha256(request.Password))
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(
                    "Invalid email or password.",
                    "البريد الإلكتروني أو كلمة المرور غير صحيحة");
            }

            if (!string.Equals(user.Status.Name, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(
                    "This account is not active.",
                    "هذا الحساب غير مفعل");
            }

            var nowUtc = DateTimeOffset.UtcNow;
            user.LastLoginAtUtc = nowUtc;
            await _db.SaveChangesAsync(cancellationToken);

            var accessTokenMinutes = _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 120;
            var expiresAtUtc = nowUtc.AddMinutes(accessTokenMinutes);
            var accessToken = GenerateAccessToken(user, expiresAtUtc);

            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                ExpiresAtUtc = expiresAtUtc,
                User = MapToAuthUserDto(user)
            };

            return ApiResponse<LoginResponseDto>.SuccessResponse(
                response,
                "Login successful.",
                "تم تسجيل الدخول بنجاح");
        }

        public async Task<ApiResponse<AuthUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user == null)
            {
                return ApiResponse<AuthUserDto>.FailureResponse(
                    "User was not found.",
                    "المستخدم غير موجود");
            }

            return ApiResponse<AuthUserDto>.SuccessResponse(MapToAuthUserDto(user));
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user == null)
            {
                return ApiResponse<bool>.FailureResponse("User was not found.", "المستخدم غير موجود");
            }

            if (user.PasswordHash != SecurityHashHelper.Sha256(request.CurrentPassword))
            {
                return ApiResponse<bool>.FailureResponse(
                    "Current password is incorrect.",
                    "كلمة المرور الحالية غير صحيحة");
            }

            user.PasswordHash = SecurityHashHelper.Sha256(request.NewPassword);
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<bool>.SuccessResponse(true, "Password changed successfully.", "تم تغيير كلمة المرور بنجاح");
        }

        private static AuthUserDto MapToAuthUserDto(User user)
        {
            return new AuthUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role.Name,
                OrganizationId = user.OrganizationId,
                UserTypeId = user.UserTypeId
            };
        }

        private string GenerateAccessToken(User user, DateTimeOffset expiresAtUtc)
        {
            var secretKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey configuration is missing.");
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("role", user.Role.Name),
                new("roleId", user.RoleId.ToString()),
                new("organizationId", user.OrganizationId.ToString()),
                new("userTypeId", user.UserTypeId.ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc.UtcDateTime,
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
