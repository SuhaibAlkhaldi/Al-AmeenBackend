using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Auth;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DLPManagementSystem.Service.Service
{
    public class AuthService : IAuthService
    {
        private const int MaxFailedLoginAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly DLPSystemContext _db;
        private readonly IConfiguration _configuration;
        private readonly IPasswordService _passwordService;
        private readonly IAdminAuditLogService _adminAuditLogService;

        public AuthService(
            DLPSystemContext db,
            IConfiguration configuration,
            IPasswordService passwordService,
            IAdminAuditLogService adminAuditLogService)
        {
            _db = db;
            _configuration = configuration;
            _passwordService = passwordService;
            _adminAuditLogService = adminAuditLogService;
        }

        public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users
                .Include(x => x.Role)
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

            if (user == null)
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(
                    "Invalid email or password.",
                    "البريد الإلكتروني أو كلمة المرور غير صحيحة");
            }

            var nowUtc = DateTimeOffset.UtcNow;

            // Locked-out accounts are rejected before the password is even checked, and a lockout in
            // progress is never extended by further attempts against it.
            if (user.LockedOutUntilUtc.HasValue && user.LockedOutUntilUtc.Value > nowUtc)
            {
                return ApiResponse<LoginResponseDto>.FailureResponse(
                    "This account is temporarily locked due to repeated failed sign-in attempts. Please try again later.",
                    "تم قفل هذا الحساب مؤقتًا بسبب تكرار محاولات تسجيل الدخول الفاشلة. يرجى المحاولة لاحقًا");
            }

            if (!_passwordService.VerifyAndUpgrade(user, request.Password))
            {
                user.FailedLoginAttemptCount++;

                if (user.FailedLoginAttemptCount >= MaxFailedLoginAttempts)
                {
                    user.LockedOutUntilUtc = nowUtc.Add(LockoutDuration);
                    user.FailedLoginAttemptCount = 0;

                    // Actor is the affected account itself: this is a system-triggered security event, not
                    // an action a human admin performed, but it's exactly the kind of admin-relevant record
                    // this log exists for (someone should be able to see "this account got locked out and
                    // when"). Modeling the account as its own actor keeps the log's existing actor-lookup
                    // logic (which resolves email/name/role from ActorUserId) working unmodified.
                    await _adminAuditLogService.LogAsync(
                        user.OrganizationId, user.Id, "AccountLockedOut", "User", user.Id, user.FullName,
                        $"Locked out until {user.LockedOutUntilUtc:o} UTC after {MaxFailedLoginAttempts} consecutive failed login attempts.",
                        cancellationToken);
                }

                await _db.SaveChangesAsync(cancellationToken);

                return ApiResponse<LoginResponseDto>.FailureResponse(
                    "Invalid email or password.",
                    "البريد الإلكتروني أو كلمة المرور غير صحيحة");
            }

            if (!string.Equals(user.Status.Name, "Active", StringComparison.OrdinalIgnoreCase))
            {
                // Password was correct (and VerifyAndUpgrade may have already rewritten PasswordHash to the
                // new format in memory) - persist that upgrade even though login itself is being rejected,
                // so a correctly-authenticated-but-inactive account still converges to the new hash format
                // instead of silently losing the upgrade opportunity every time.
                await _db.SaveChangesAsync(cancellationToken);

                return ApiResponse<LoginResponseDto>.FailureResponse(
                    "This account is not active.",
                    "هذا الحساب غير مفعل");
            }

            user.FailedLoginAttemptCount = 0;
            user.LockedOutUntilUtc = null;
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

            if (!_passwordService.VerifyAndUpgrade(user, request.CurrentPassword))
            {
                return ApiResponse<bool>.FailureResponse(
                    "Current password is incorrect.",
                    "كلمة المرور الحالية غير صحيحة");
            }

            if (request.NewPassword.Length < PasswordPolicy.MinLength)
            {
                return ApiResponse<bool>.FailureResponse(PasswordPolicy.MessageEn, PasswordPolicy.MessageAr);
            }

            user.PasswordHash = _passwordService.HashPassword(user, request.NewPassword);
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;
            // The account holder just set their own password - the forced-change requirement (if any)
            // is satisfied regardless of why it was set (new account, admin reset, or a voluntary
            // change), so this always clears it.
            user.MustChangePassword = false;

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
                UserTypeId = user.UserTypeId,
                MustChangePassword = user.MustChangePassword
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
