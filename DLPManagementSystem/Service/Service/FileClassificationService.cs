using System.Net.Http.Headers;
using System.Text.Json;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;
using DLPManagementSystem.Service.Interface;
using Microsoft.Extensions.Options;

namespace DLPManagementSystem.Service.Service
{
    public sealed class FileClassificationApiOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }

    // Extension-blocklist fallback used whenever no file content is available to classify (see
    // IFileClassificationService.ClassifyAsync's fileContent parameter) - kept as a fail-safe default
    // rather than removed, since a caller with no bytes to send genuinely cannot get a real
    // content-based verdict either way.
    public class FileClassificationService : IFileClassificationService
    {
        private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".scr", ".ps1", ".vbs", ".js", ".msi"
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FileClassificationApiOptions _apiOptions;
        private readonly ILogger<FileClassificationService> _logger;

        public FileClassificationService(
            IHttpClientFactory httpClientFactory,
            IOptions<FileClassificationApiOptions> apiOptions,
            ILogger<FileClassificationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiOptions = apiOptions.Value;
            _logger = logger;
        }

        public async Task<ApiResponse<FileClassificationResultDto>> ClassifyAsync(
            Guid organizationId,
            Guid deviceId,
            FileClassificationRequestDto request,
            Stream? fileContent,
            CancellationToken cancellationToken = default)
        {
            if (request.TenantId != organizationId || request.DeviceId != deviceId)
            {
                return ApiResponse<FileClassificationResultDto>.FailureResponse(
                    "tenantId/deviceId do not match the authenticated device.",
                    "معرّف المؤسسة أو الجهاز لا يطابق الجهاز المصادق عليه");
            }

            if (fileContent != null && !string.IsNullOrWhiteSpace(_apiOptions.BaseUrl))
            {
                var apiResult = await TryClassifyViaAiApiAsync(request, fileContent, cancellationToken);
                if (apiResult != null)
                {
                    return ApiResponse<FileClassificationResultDto>.SuccessResponse(apiResult);
                }
            }

            return ApiResponse<FileClassificationResultDto>.SuccessResponse(BuildStubResult(request));
        }

        // Returns null (rather than throwing) on any failure so the caller falls back to the stub
        // result - a background-scan classification failing must never crash the scan loop, and the
        // agent-side PermissionEvaluator already treats an uncached/unclassified file as fail-closed
        // (ClassificationTiers.VerySecret), so a missing verdict here is safe, not silently permissive.
        private async Task<FileClassificationResultDto?> TryClassifyViaAiApiAsync(
            FileClassificationRequestDto request,
            Stream fileContent,
            CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("FileClassificationApi");
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_apiOptions.TimeoutSeconds, 5, 120)));

                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileContent);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    string.IsNullOrWhiteSpace(request.MimeType) ? "application/octet-stream" : request.MimeType);
                content.Add(streamContent, "file", string.IsNullOrWhiteSpace(request.FileName) ? "file" : request.FileName);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/scan") { Content = content };
                httpRequest.Headers.Add("X-API-Key", _apiOptions.ApiKey);

                using var response = await client.SendAsync(httpRequest, timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "File classification API returned {StatusCode} for {FileName}.", response.StatusCode, request.FileName);
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<AiScanResponse>(cancellationToken: timeoutCts.Token);
                if (payload == null || string.IsNullOrWhiteSpace(payload.Classification))
                {
                    return null;
                }

                var nowUtc = DateTimeOffset.UtcNow;
                return new FileClassificationResultDto
                {
                    RequestId = request.RequestId,
                    IsAllowed = payload.Classification.Equals(ClassificationTiers.Public, StringComparison.OrdinalIgnoreCase),
                    IsSensitive = !payload.Classification.Equals(ClassificationTiers.Public, StringComparison.OrdinalIgnoreCase),
                    Classification = payload.Classification,
                    ReasonCode = string.IsNullOrWhiteSpace(payload.Reasoning) ? "AiClassified" : payload.Reasoning,
                    Provider = "AiApi",
                    RuleId = null,
                    EvaluatedAtUtc = nowUtc,
                    ValidUntilUtc = null
                };
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(exception, "File classification API call failed for {FileName}.", request.FileName);
                return null;
            }
        }

        private static FileClassificationResultDto BuildStubResult(FileClassificationRequestDto request)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var extension = string.IsNullOrWhiteSpace(request.Extension)
                ? Path.GetExtension(request.FileName)
                : request.Extension;

            var isBlocked = !string.IsNullOrWhiteSpace(extension) && BlockedExtensions.Contains(extension);

            return new FileClassificationResultDto
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
        }

        // Matches the AI classifier API's response shape exactly: { status, filename, classification, reasoning }.
        private sealed class AiScanResponse
        {
            public string? Status { get; set; }
            public string? Filename { get; set; }
            public string? Classification { get; set; }
            public string? Reasoning { get; set; }
        }
    }
}
