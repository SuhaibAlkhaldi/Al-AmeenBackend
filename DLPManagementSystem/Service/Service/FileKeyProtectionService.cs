using System.Security.Cryptography;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.DataProtection;

namespace DLPManagementSystem.Service.Service
{
    public class FileKeyProtectionService : IFileKeyProtectionService
    {
        private readonly IDataProtector _protector;

        public FileKeyProtectionService(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("CompanyDlp.FileKeys");
        }

        public ApiResponse<FileKeyWrapResponseDto> Wrap(FileKeyWrapRequestDto request)
        {
            byte[] plainKeyBytes;

            try
            {
                plainKeyBytes = Convert.FromBase64String(request.PlainKeyBase64);
            }
            catch (FormatException)
            {
                return ApiResponse<FileKeyWrapResponseDto>.FailureResponse(
                    "plainKeyBase64 is not valid base64.",
                    "قيمة plainKeyBase64 غير صحيحة");
            }

            var wrappedKeyBytes = _protector.Protect(plainKeyBytes);

            var response = new FileKeyWrapResponseDto
            {
                KeyId = Guid.NewGuid().ToString(),
                WrappedKeyBase64 = Convert.ToBase64String(wrappedKeyBytes)
            };

            return ApiResponse<FileKeyWrapResponseDto>.SuccessResponse(response);
        }

        public ApiResponse<FileKeyUnwrapResponseDto> Unwrap(FileKeyUnwrapRequestDto request)
        {
            byte[] wrappedKeyBytes;

            try
            {
                wrappedKeyBytes = Convert.FromBase64String(request.WrappedKeyBase64);
            }
            catch (FormatException)
            {
                return ApiResponse<FileKeyUnwrapResponseDto>.FailureResponse(
                    "wrappedKeyBase64 is not valid base64.",
                    "قيمة wrappedKeyBase64 غير صحيحة");
            }

            byte[] plainKeyBytes;

            try
            {
                plainKeyBytes = _protector.Unprotect(wrappedKeyBytes);
            }
            catch (CryptographicException)
            {
                return ApiResponse<FileKeyUnwrapResponseDto>.FailureResponse(
                    "The wrapped key could not be unwrapped (invalid or tampered payload).",
                    "تعذر فك تشفير المفتاح");
            }

            var response = new FileKeyUnwrapResponseDto
            {
                PlainKeyBase64 = Convert.ToBase64String(plainKeyBytes)
            };

            return ApiResponse<FileKeyUnwrapResponseDto>.SuccessResponse(response);
        }
    }
}
