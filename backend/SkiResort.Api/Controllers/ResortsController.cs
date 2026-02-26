using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Models;
using SkiResort.Infrastructure.Data;

namespace SkiResort.Api.Controllers;

[ApiController]
[Route("api/resorts")]
public class ResortsController : ControllerBase
{
    private const int DefaultRunHistoryPageSize = 50;
    private const int MaxRunHistoryPageSize = 200;

    private readonly SkiResortDbContext _dbContext;

    public ResortsController(SkiResortDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns all resorts.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResortDto>>> GetResorts(CancellationToken cancellationToken)
    {
        var resorts = await _dbContext.Resorts
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new ResortDto(
                r.Id,
                r.Name,
                r.Region,
                r.Country,
                r.ElevationBaseMeters,
                r.ElevationTopMeters))
            .ToListAsync(cancellationToken);

        return Ok(resorts);
    }

    /// <summary>
    /// Returns resort details, latest conditions, and keyset-paginated run history.
    /// </summary>
    [HttpGet("{id:guid}/conditions")]
    public async Task<ActionResult<ResortConditionsResponse>> GetResortConditions(
        Guid id,
        [FromQuery] DateTimeOffset? cursorUpdatedBefore,
        [FromQuery] Guid? cursorIdBefore,
        [FromQuery] int pageSize = DefaultRunHistoryPageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0 || pageSize > MaxRunHistoryPageSize)
        {
            pageSize = DefaultRunHistoryPageSize;
        }

        var resort = await _dbContext.Resorts
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (resort is null)
        {
            return NotFound();
        }

        var resortDto = new ResortDto(
            resort.Id,
            resort.Name,
            resort.Region,
            resort.Country,
            resort.ElevationBaseMeters,
            resort.ElevationTopMeters);

        var latestSnow = await _dbContext.SnowConditions
            .AsNoTracking()
            .Where(c => c.ResortId == id)
            .OrderByDescending(c => c.ObservedAt)
            .FirstOrDefaultAsync(cancellationToken);

        SnowConditionDto? latestSnowDto = latestSnow is null
            ? null
            : new SnowConditionDto(
                latestSnow.Id,
                latestSnow.ResortId,
                latestSnow.ObservedAt,
                latestSnow.SnowDepthCm,
                latestSnow.NewSnowCm);

        var liftSnapshots = await _dbContext.LiftStatuses
            .AsNoTracking()
            .Where(l => l.ResortId == id)
            .OrderByDescending(l => l.UpdatedAt)
            .ToListAsync(cancellationToken);

        var currentLifts = liftSnapshots
            .GroupBy(l => l.Name)
            .Select(g => g.OrderByDescending(l => l.UpdatedAt).First())
            .OrderBy(l => l.Name)
            .Select(l => new LiftStatusDto(
                l.Id,
                l.ResortId,
                l.Name,
                l.IsOpen,
                l.UpdatedAt))
            .ToList();

        var runQuery = _dbContext.RunStatuses
            .AsNoTracking()
            .Where(r => r.ResortId == id)
            .OrderByDescending(r => r.UpdatedAt)
            .ThenByDescending(r => r.Id);

        if (cursorUpdatedBefore.HasValue)
        {
            if (cursorIdBefore.HasValue)
            {
                var updatedAt = cursorUpdatedBefore.Value;
                var lastId = cursorIdBefore.Value;

                runQuery = runQuery.Where(r =>
                    r.UpdatedAt < updatedAt ||
                    (r.UpdatedAt == updatedAt && string.CompareOrdinal(r.Id.ToString(), lastId.ToString(), StringComparison.Ordinal) < 0));
            }
            else
            {
                runQuery = runQuery.Where(r => r.UpdatedAt < cursorUpdatedBefore.Value);
            }
        }

        var runItems = await runQuery
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        DateTimeOffset? nextUpdatedBefore = null;
        Guid? nextIdBefore = null;

        if (runItems.Count == pageSize)
        {
            var last = runItems[^1];
            nextUpdatedBefore = last.UpdatedAt;
            nextIdBefore = last.Id;
        }

        var runDtos = runItems
            .Select(r => new RunStatusDto(
                r.Id,
                r.ResortId,
                r.Name,
                r.IsOpen,
                r.UpdatedAt))
            .ToList();

        var response = new ResortConditionsResponse
        {
            Resort = resortDto,
            LatestSnowCondition = latestSnowDto,
            CurrentLiftStatuses = currentLifts,
            RunStatusPage = new RunStatusPageDto
            {
                Items = runDtos,
                NextUpdatedBefore = nextUpdatedBefore,
                NextIdBefore = nextIdBefore
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Streams the full snow history for a resort as an async sequence.
    /// </summary>
    [HttpGet("{id:guid}/snow-history/stream")]
    public async IAsyncEnumerable<SnowConditionDto> StreamSnowHistory(
        Guid id,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SnowConditions
            .AsNoTracking()
            .Where(c => c.ResortId == id)
            .OrderBy(c => c.ObservedAt)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var condition in query)
        {
            yield return new SnowConditionDto(
                condition.Id,
                condition.ResortId,
                condition.ObservedAt,
                condition.SnowDepthCm,
                condition.NewSnowCm);
        }
    }
}

