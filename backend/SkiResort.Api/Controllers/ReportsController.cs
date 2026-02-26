using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SkiResort.Infrastructure.Reports;

namespace SkiResort.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ISnowComparisonReportRepository _repository;

    public ReportsController(ISnowComparisonReportRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Returns a snow comparison report for the specified resorts over the last 7 days.
    /// </summary>
    [HttpGet("snow-comparison")]
    public async Task<ActionResult<IReadOnlyList<SnowComparisonRow>>> GetSnowComparison(
        [FromQuery] Guid[] resortIds,
        CancellationToken cancellationToken)
    {
        if (resortIds == null || resortIds.Length == 0)
        {
            return BadRequest("At least one resortId must be specified.");
        }

        var rows = await _repository.GetSnowComparisonAsync(resortIds, cancellationToken);
        return Ok(rows);
    }
}

