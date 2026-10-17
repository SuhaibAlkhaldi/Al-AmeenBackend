using System.ComponentModel.DataAnnotations;
using DLPManagementSystem.CompanyDlpDashboard.Rollup;
using DLPManagementSystem.Common;
using DLPManagementSystem.Models;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DLPManagementSystem.Controllers;

// [EnumDataType] rejects an out-of-range Kind with a clean 400 via [ApiController]'s automatic
// model validation - without it, an invalid int reaches DashboardRollupRefreshService's switch
// and surfaces as an unhandled 500 (ArgumentOutOfRangeException) instead.
public sealed record DashboardRollupBackfillRequestDto(
    [EnumDataType(typeof(DashboardRollupKindSelector))] DashboardRollupKindSelector Kind,
    int MaxChunks = 1);

public sealed record DashboardRollupBackfillChunkDto(bool MoreRemaining, DateTimeOffset NewWatermarkUtc, long RowsWritten);

public sealed record DashboardRollupBackfillResponseDto(
    string Kind,
    int ChunksProcessed,
    bool MoreRemaining,
    DateTimeOffset NewWatermarkUtc,
    long TotalRowsWritten,
    IReadOnlyList<DashboardRollupBackfillChunkDto> Chunks);

public sealed record DashboardRollupWatermarkDto(string RollupKind, DateTimeOffset LastComputedThroughUtc, DateTimeOffset UpdatedAtUtc);

// dashboard-rollup: manual, SuperAdmin-only control surface for the one-time historical backfill
// (see DashboardRollupRefreshService.BackfillOneChunkAsync) - never runs automatically, never on
// app startup, and is NOT an EF migration (building/backfilling against a large AuditEvents table
// must happen deliberately, during a maintenance window, not block or slow down every app start).
// Scoped to the caller's own organization (User.GetOrganizationId()) - same multi-tenant
// convention every other controller in this app already follows - never an organizationId taken
// from the request body/query.
[ApiController]
[Route("api/v1/admin/dashboard-rollup")]
[Authorize(Roles = "SuperAdmin")]
public sealed class DashboardRollupAdminController : ControllerBase
{
    // One HTTP call must still return in a reasonable time - a caller wanting to backfill years
    // of history issues several calls (each resuming from the previous one's watermark) rather
    // than one call processing an unbounded number of chunks.
    private const int MaxChunksPerRequest = 30;

    private readonly IDashboardRollupRefreshService _refreshService;
    private readonly IAdminAuditLogService _adminAuditLogService;
    private readonly DLPSystemContext _db;

    public DashboardRollupAdminController(
        IDashboardRollupRefreshService refreshService,
        IAdminAuditLogService adminAuditLogService,
        DLPSystemContext db)
    {
        _refreshService = refreshService;
        _adminAuditLogService = adminAuditLogService;
        _db = db;
    }

    /// <summary>Processes up to <paramref name="request"/>.MaxChunks backfill chunks (each
    /// BackfillChunkDays wide - see DashboardRollup options) for one rollup kind, resuming from
    /// this organization's current watermark. Call repeatedly (MoreRemaining tells you when to
    /// stop) to backfill further into the past.</summary>
    [HttpPost("backfill")]
    public async Task<ActionResult<DashboardRollupBackfillResponseDto>> Backfill(
        [FromBody] DashboardRollupBackfillRequestDto request,
        CancellationToken cancellationToken)
    {
        var organizationId = User.GetOrganizationId();
        var actorUserId = User.GetUserId();
        var maxChunks = Math.Clamp(request.MaxChunks, 1, MaxChunksPerRequest);

        var chunks = new List<DashboardRollupBackfillChunkDto>();
        long totalRows = 0;
        var moreRemaining = true;
        var lastWatermark = DateTimeOffset.UtcNow;

        for (var i = 0; i < maxChunks && moreRemaining; i++)
        {
            var result = await _refreshService.BackfillOneChunkAsync(organizationId, request.Kind, cancellationToken);
            chunks.Add(new DashboardRollupBackfillChunkDto(result.MoreRemaining, result.NewWatermarkUtc, result.RowsWritten));
            totalRows += result.RowsWritten;
            moreRemaining = result.MoreRemaining;
            lastWatermark = result.NewWatermarkUtc;

            if (result.RowsWritten == 0 && !result.MoreRemaining)
            {
                break;
            }
        }

        await _adminAuditLogService.LogAsync(
            organizationId,
            actorUserId,
            actionType: "DashboardRollupBackfill",
            targetType: "DashboardRollup",
            targetId: null,
            targetDisplayName: request.Kind.ToString(),
            details: $"Processed {chunks.Count} chunk(s), {totalRows} row(s) written, watermark now {lastWatermark:o}, moreRemaining={moreRemaining}.",
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new DashboardRollupBackfillResponseDto(request.Kind.ToString(), chunks.Count, moreRemaining, lastWatermark, totalRows, chunks));
    }

    /// <summary>Current watermark (how far the rollup has been computed) per kind for this
    /// organization - lets an admin see backfill/refresh progress without guessing.</summary>
    [HttpGet("watermarks")]
    public async Task<ActionResult<IReadOnlyList<DashboardRollupWatermarkDto>>> GetWatermarks(CancellationToken cancellationToken)
    {
        var organizationId = User.GetOrganizationId();

        var watermarks = await _db.DashboardRollupWatermarks
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new DashboardRollupWatermarkDto(x.RollupKind, x.LastComputedThroughUtc, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(watermarks);
    }
}
