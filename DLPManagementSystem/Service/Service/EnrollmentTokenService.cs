using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.EnrollmentTokens;
using DLPManagementSystem.Helper.Hashing;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class EnrollmentTokenService : IEnrollmentTokenService
    {
        private const int DefaultExpiresInDays = 30;
        private const int DefaultMaxUses = 1;

        private readonly DLPSystemContext _db;

        public EnrollmentTokenService(DLPSystemContext db)
        {
            _db = db;
        }

        public async Task<ApiResponse<PagedResultDto<EnrollmentTokenDto>>> GetTokensAsync(
            Guid organizationId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _db.AgentEnrollmentTokens
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EnrollmentTokenDto
                {
                    Id = x.Id,
                    DisplayName = x.DisplayName,
                    ExpiresAtUtc = x.ExpiresAtUtc,
                    MaxUses = x.MaxUses,
                    UsedCount = x.UsedCount,
                    RevokedAtUtc = x.RevokedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    CreatedByUserId = x.CreatedByUserId,
                    CreatedByUserName = x.CreatedByUser.FullName
                })
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<EnrollmentTokenDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<EnrollmentTokenDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse<EnrollmentTokenCreatedDto>> CreateTokenAsync(
            Guid organizationId,
            Guid createdByUserId,
            CreateEnrollmentTokenDto request,
            CancellationToken cancellationToken = default)
        {
            var expiresInDays = request.ExpiresInDays ?? DefaultExpiresInDays;
            var maxUses = request.MaxUses ?? DefaultMaxUses;
            var nowUtc = DateTimeOffset.UtcNow;

            var plainTextCode = SecurityHashHelper.GenerateSecret();
            var codeHash = SecurityHashHelper.Sha256(plainTextCode);

            var token = new AgentEnrollmentToken
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TokenHash = codeHash,
                DisplayName = request.DisplayName,
                ExpiresAtUtc = nowUtc.AddDays(expiresInDays),
                MaxUses = maxUses,
                UsedCount = 0,
                CreatedByUserId = createdByUserId,
                CreatedAtUtc = nowUtc,
                RevokedAtUtc = null
            };

            _db.AgentEnrollmentTokens.Add(token);
            await _db.SaveChangesAsync(cancellationToken);

            var createdByUser = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == createdByUserId, cancellationToken);

            var dto = new EnrollmentTokenCreatedDto
            {
                Id = token.Id,
                DisplayName = token.DisplayName,
                ExpiresAtUtc = token.ExpiresAtUtc,
                MaxUses = token.MaxUses,
                UsedCount = token.UsedCount,
                RevokedAtUtc = token.RevokedAtUtc,
                CreatedAtUtc = token.CreatedAtUtc,
                CreatedByUserId = token.CreatedByUserId,
                CreatedByUserName = createdByUser?.FullName ?? string.Empty,
                PlainTextCode = plainTextCode
            };

            return ApiResponse<EnrollmentTokenCreatedDto>.SuccessResponse(
                dto,
                "Enrollment token created successfully.",
                "تم إنشاء رمز التسجيل بنجاح");
        }

        public async Task<ApiResponse<object?>> RevokeTokenAsync(
            Guid organizationId,
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var token = await _db.AgentEnrollmentTokens
                .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);

            if (token == null)
            {
                return ApiResponse<object?>.FailureResponse("Enrollment token was not found.", "رمز التسجيل غير موجود");
            }

            if (token.RevokedAtUtc != null)
            {
                return ApiResponse<object?>.FailureResponse("Enrollment token is already revoked.", "تم إلغاء رمز التسجيل مسبقًا");
            }

            token.RevokedAtUtc = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<object?>.SuccessResponse(null, "Enrollment token revoked successfully.", "تم إلغاء رمز التسجيل بنجاح");
        }
    }
}
