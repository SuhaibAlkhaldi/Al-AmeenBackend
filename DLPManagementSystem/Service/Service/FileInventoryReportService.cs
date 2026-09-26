using System.Linq.Expressions;
using ClosedXML.Excel;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.AgentFiles;
using DLPManagementSystem.DTO.FileInventory;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Service.Service
{
    public class FileInventoryReportService : IFileInventoryReportService
    {
        // Same cap/convention as AuditEventService.MaxExportRows.
        private const int MaxExportRows = 50_000;

        private readonly DLPSystemContext _db;

        public FileInventoryReportService(DLPSystemContext db)
        {
            _db = db;
        }

        private IQueryable<FileInventoryRecord> BuildFilteredQuery(
            Guid organizationId,
            List<Guid>? employeeIds,
            List<string>? classifications,
            List<string>? provenances,
            List<int>? fileTypeIds,
            bool? isProtected,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc)
        {
            var query = _db.FileInventoryRecords
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && !x.IsDeleted);

            if (employeeIds is { Count: > 0 })
            {
                query = query.Where(x => x.EmployeeId != null && employeeIds.Contains(x.EmployeeId.Value));
            }

            if (classifications is { Count: > 0 })
            {
                query = query.Where(x => classifications.Contains(x.ClassificationTier));
            }

            if (provenances is { Count: > 0 })
            {
                query = query.Where(x => provenances.Contains(x.Provenance));
            }

            if (fileTypeIds is { Count: > 0 })
            {
                query = query.Where(x => x.FileTypeId != null && fileTypeIds.Contains(x.FileTypeId.Value));
            }

            if (isProtected.HasValue)
            {
                query = query.Where(x => x.IsProtected == isProtected.Value);
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.LastScanAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.LastScanAtUtc <= toUtc.Value);
            }

            return query;
        }

        // Agreed default sort: Employee, then Classification by real severity (Public < Internal <
        // Secret < Very_Secret), not alphabetically - a plain string sort would put "Internal" before
        // "Public" and visibly misorder the column for an admin. Written as a ternary chain (rather than
        // ClassificationTiers.Order.IndexOf(...)) so EF Core can translate it to a SQL CASE expression.
        private static IOrderedQueryable<FileInventoryRecord> ApplyDefaultSort(IQueryable<FileInventoryRecord> query)
        {
            return query
                .OrderBy(x => x.Employee != null ? x.Employee.DisplayName : null)
                .ThenBy(x => x.ClassificationTier == ClassificationTiers.Public ? 0
                    : x.ClassificationTier == ClassificationTiers.Internal ? 1
                    : x.ClassificationTier == ClassificationTiers.Secret ? 2
                    : x.ClassificationTier == ClassificationTiers.VerySecret ? 3
                    : 4);
        }

        public async Task<ApiResponse<PagedResultDto<FileInventoryReportItemDto>>> GetFileInventoryAsync(
            Guid organizationId,
            List<Guid>? employeeIds,
            List<string>? classifications,
            List<string>? provenances,
            List<int>? fileTypeIds,
            bool? isProtected,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = BuildFilteredQuery(organizationId, employeeIds, classifications, provenances, fileTypeIds, isProtected, fromUtc, toUtc);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await ApplyDefaultSort(query)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ProjectToItemDto)
                .ToListAsync(cancellationToken);

            var result = new PagedResultDto<FileInventoryReportItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return ApiResponse<PagedResultDto<FileInventoryReportItemDto>>.SuccessResponse(result);
        }

        public async Task ExportFileInventoryAsync(
            Guid organizationId,
            List<Guid>? employeeIds,
            List<string>? classifications,
            List<string>? provenances,
            List<int>? fileTypeIds,
            bool? isProtected,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            HttpResponse response,
            CancellationToken cancellationToken = default)
        {
            var query = BuildFilteredQuery(organizationId, employeeIds, classifications, provenances, fileTypeIds, isProtected, fromUtc, toUtc);

            var totalMatching = await query.CountAsync(cancellationToken);
            var truncated = totalMatching > MaxExportRows;

            var items = await ApplyDefaultSort(query)
                .Take(MaxExportRows)
                .Select(ProjectToItemDto)
                .ToListAsync(cancellationToken);

            // Unlike the CSV export elsewhere in this codebase (line-by-line StreamWriter), a real .xlsx
            // needs a fully-built workbook before it can be written out - ClosedXML builds it in memory
            // here, capped at MaxExportRows same as the CSV convention, so this stays bounded.
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("File Inventory");

            string[] headers =
            [
                "Employee", "Device", "File Name", "Path", "Classification",
                "File Type", "Size (bytes)", "Source", "Protection", "Last Scan (UTC)"
            ];
            for (var col = 0; col < headers.Length; col++)
            {
                sheet.Cell(1, col + 1).Value = headers[col];
            }
            sheet.Row(1).Style.Font.Bold = true;

            var row = 2;
            foreach (var item in items)
            {
                sheet.Cell(row, 1).Value = item.EmployeeName ?? string.Empty;
                sheet.Cell(row, 2).Value = item.DeviceName;
                sheet.Cell(row, 3).Value = item.FileName;
                sheet.Cell(row, 4).Value = item.FilePath;
                sheet.Cell(row, 5).Value = item.ClassificationTier;
                sheet.Cell(row, 6).Value = item.FileTypeName ?? string.Empty;
                sheet.Cell(row, 7).Value = item.SizeBytes;
                sheet.Cell(row, 8).Value = item.Provenance;
                sheet.Cell(row, 9).Value = item.IsProtected ? "Protected" : "Unprotected";
                sheet.Cell(row, 10).Value = item.LastScanAtUtc.ToString("o");
                row++;
            }

            sheet.Columns().AdjustToContents();

            var fileName = $"file-inventory-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.xlsx";
            response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            if (truncated)
            {
                response.Headers["X-Export-Truncated"] = "true";
            }

            // ClosedXML's SaveAs writes to the stream with plain synchronous Stream.Write calls
            // internally - fine against a MemoryStream (as every earlier in-process test of this method
            // used), but Kestrel's real HttpResponse.Body disallows synchronous I/O by default and
            // throws InvalidOperationException("Synchronous operations are disallowed...") the moment
            // this runs behind an actual live request. Save to a buffer first (sync, no Kestrel stream
            // involved) then copy that buffer to the response asynchronously.
            using var buffer = new MemoryStream();
            workbook.SaveAs(buffer);
            buffer.Position = 0;
            await buffer.CopyToAsync(response.Body, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }

        // Kept as an Expression<Func<...>> (not a compiled method called from inside Select) so EF Core
        // can translate the navigation-property access (x.Device.MachineName etc.) into SQL joins and
        // project only the needed columns - a plain method reference here would force EF to fetch bare
        // FileInventoryRecord rows with null navigations and NullReferenceException on materialization,
        // exactly the bug this shape avoids. Shared between the paged list and the export so both stay
        // in sync with the same column mapping.
        private static readonly Expression<Func<FileInventoryRecord, FileInventoryReportItemDto>> ProjectToItemDto = x => new FileInventoryReportItemDto
        {
            FileId = x.FileId,
            EmployeeName = x.Employee != null ? x.Employee.DisplayName : null,
            DeviceName = x.Device.MachineName,
            FileName = x.FileName,
            FilePath = x.FilePath,
            ClassificationTier = x.ClassificationTier,
            FileTypeName = x.FileType != null ? x.FileType.Name : x.Extension,
            SizeBytes = x.SizeBytes,
            Provenance = x.Provenance,
            IsProtected = x.IsProtected,
            LastScanAtUtc = x.LastScanAtUtc
        };
    }
}
