using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Microsoft.AspNetCore.Mvc;
using SkiResort.Api.Models;
using SkiResort.Domain.Entities;
using SkiResort.Infrastructure.Favorites;

namespace SkiResort.Api.Controllers;

[ApiController]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly IUserFavoritesRepository _repository;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(
        IUserFavoritesRepository repository,
        ILogger<FavoritesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current user's favorite resort IDs.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserFavoriteDto>>> GetFavorites(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var favorites = await _repository.GetFavoritesAsync(userId, cancellationToken);

        var dtos = new List<UserFavoriteDto>(favorites.Count);
        foreach (var favorite in favorites)
        {
            dtos.Add(new UserFavoriteDto(favorite.UserId, favorite.ResortId));
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Adds a resort to the current user's favorites.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddFavorite(
        [FromBody] AddFavoriteRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.ResortId == Guid.Empty)
        {
            return BadRequest("A valid resortId must be provided.");
        }

        var userId = GetUserId();

        var favorite = new UserFavorite
        {
            UserId = userId,
            ResortId = request.ResortId
        };

        try
        {
            await _repository.AddFavoriteAsync(favorite, cancellationToken);
        }
        catch (AmazonServiceException ex)
        {
            _logger.LogError(ex, "DynamoDB error adding favorite for user {UserId}", userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Favorites storage is unavailable. Check ECS task role DynamoDB permissions and table name." });
        }

        return NoContent();
    }

    /// <summary>
    /// Convenience route for clients that POST the resortId in the URL.
    /// </summary>
    [HttpPost("{resortId:guid}")]
    public async Task<IActionResult> AddFavoriteById(
        Guid resortId,
        CancellationToken cancellationToken)
    {
        if (resortId == Guid.Empty)
        {
            return BadRequest("A valid resortId must be provided.");
        }

        var userId = GetUserId();
        var favorite = new UserFavorite
        {
            UserId = userId,
            ResortId = resortId
        };

        try
        {
            await _repository.AddFavoriteAsync(favorite, cancellationToken);
        }
        catch (AmazonServiceException ex)
        {
            _logger.LogError(ex, "DynamoDB error adding favorite for user {UserId}", userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Favorites storage is unavailable. Check ECS task role DynamoDB permissions and table name." });
        }

        return NoContent();
    }

    /// <summary>
    /// Removes a resort from the current user's favorites.
    /// </summary>
    [HttpDelete("{resortId:guid}")]
    public async Task<IActionResult> RemoveFavorite(
        Guid resortId,
        CancellationToken cancellationToken)
    {
        if (resortId == Guid.Empty)
        {
            return BadRequest("A valid resortId must be provided.");
        }

        var userId = GetUserId();
        try
        {
            await _repository.RemoveFavoriteAsync(userId, resortId, cancellationToken);
        }
        catch (AmazonServiceException ex)
        {
            _logger.LogError(ex, "DynamoDB error removing favorite for user {UserId}", userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Favorites storage is unavailable. Check ECS task role DynamoDB permissions and table name." });
        }

        return NoContent();
    }

    private string GetUserId()
    {
        // For now we use a simple header-based user identifier.
        // This can be replaced with a proper auth/session-based implementation later.
        var headerUserId = HttpContext.Request.Headers["X-User-Id"].ToString();
        return string.IsNullOrWhiteSpace(headerUserId) ? "demo-user" : headerUserId;
    }
}

