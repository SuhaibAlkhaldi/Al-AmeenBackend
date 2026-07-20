using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;
using DLPManagementSystem.Service.Interface;

namespace DLPManagementSystem.Service.Service
{
    // MVP stub: default-allow unless the file extension is on an obviously-blocked list.
    // TODO: replace with real content-inspection logic driven by the org's SensitiveRules
    // (see AgentPolicyController's policy snapshot) once a classification provider exists.
    public class FileClassificationService : IFileClassificationService
    {
        private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".scr", ".ps1", ".vbs", ".js", ".msi"
        };

        public Task<ApiResponse<FileClassificationResultDto>> ClassifyAsync(
            Guid organizationId,
            Guid deviceId,
            FileClassificationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.TenantId != organizationId || request.DeviceId != deviceId)
            {
                return Task.FromResult(ApiResponse<FileClassificationResultDto>.FailureResponse(
                    "tenantId/deviceId do not match the authenticated device.",
                    "معرّف المؤسسة أو الجهاز لا يطابق الجهاز المصادق عليه"));
            }

            var nowUtc = DateTimeOffset.UtcNow;

            var extension = string.IsNullOrWhiteSpace(request.Extension)
                ? Path.GetExtension(request.FileName)
                : request.Extension;

            var isBlocked = !string.IsNullOrWhiteSpace(extension) && BlockedExtensions.Contains(extension);

            var result = new FileClassificationResultDto
            {
                RequestId = request.RequestId,
                IsAllowed = !isBlocked,
                IsSensitive = false,
                Classification = isBlocked ? "Blocked" : "Unclassified",
                ReasonCode = isBlocked ? "BlockedFileExtension" : "DefaultAllowStubClassification",
                Provider = "StubRulePass",
                RuleId = null,
                EvaluatedAtUtc = nowUtc,
                ValidUntilUtc = nowUtc.AddMinutes(10)
            };

            return Task.FromResult(ApiResponse<FileClassificationResultDto>.SuccessResponse(result));
        }
    }
}
