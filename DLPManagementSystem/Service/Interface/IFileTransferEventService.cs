using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.FileTransfer;

namespace DLPManagementSystem.Service.Interface
{
    public interface IFileTransferEventService
    {
        // Stores the observations that match a sensitive inventory file on the SAME device. Everything else is counted and
        // dropped. Idempotent per (SendActionId, RecipientOrdinal), so a retried batch never stores a send twice.
        Task<ApiResponse<FileTransferEventBatchResultDto>> IngestAsync(
            Guid organizationId,
            Guid deviceId,
            FileTransferEventBatchRequestDto request,
            CancellationToken cancellationToken);

        Task<ApiResponse<PagedResultDto<FileTransferEventListItemDto>>> ListAsync(
            Guid organizationId,
            Guid? fileId,
            Guid? employeeId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken);
    }
}
