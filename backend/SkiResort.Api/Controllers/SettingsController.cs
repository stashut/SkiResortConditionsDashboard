using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SkiResort.Api.Models;

namespace SkiResort.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, UserSettingsDto> Store = new();

    [HttpGet]
    public Task<ActionResult<UserSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (!Store.TryGetValue(userId, out var settings))
        {
            settings = new UserSettingsDto("imperial", null, null);
        }

        return Task.FromResult<ActionResult<UserSettingsDto>>(Ok(settings));
    }

    [HttpPost]
    public Task<IActionResult> SaveSettings(
        [FromBody] UserSettingsDto request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Task.FromResult<IActionResult>(BadRequest("Settings payload is required."));
        }

        if (string.IsNullOrWhiteSpace(request.UnitPreference))
        {
            return Task.FromResult<IActionResult>(BadRequest("unitPreference is required."));
        }

        // Keep it minimal: accept any string, but validate the known values.
        var normalized = request.UnitPreference.Trim().ToLowerInvariant();
        if (normalized != "imperial" && normalized != "metric")
        {
            return Task.FromResult<IActionResult>(BadRequest("unitPreference must be 'imperial' or 'metric'."));
        }

        var userId = GetUserId();

        var stored = new UserSettingsDto(
            normalized,
            string.IsNullOrWhiteSpace(request.RegionFilter) ? null : request.RegionFilter,
            NormalizeLastViewedResortId(request.LastViewedResortId));

        Store[userId] = stored;
        return Task.FromResult<IActionResult>(NoContent());
    }

    private string GetUserId()
    {
        // Match FavoritesController approach for now.
        var headerUserId = HttpContext.Request.Headers["X-User-Id"].ToString();
        return string.IsNullOrWhiteSpace(headerUserId) ? "demo-user" : headerUserId;
    }

    private static string? NormalizeLastViewedResortId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Accept only valid GUID strings; otherwise keep it unset.
        return Guid.TryParse(value, out _) ? value : null;
    }
}

