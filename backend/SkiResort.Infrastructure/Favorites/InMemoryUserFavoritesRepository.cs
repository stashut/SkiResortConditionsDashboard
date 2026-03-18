using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiResort.Domain.Entities;

namespace SkiResort.Infrastructure.Favorites;

/// <summary>
/// Dev-only fallback when AWS credentials are not configured.
/// </summary>
public sealed class InMemoryUserFavoritesRepository : IUserFavoritesRepository
{
    // userId -> set of resortIds
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _store = new();

    public Task<IReadOnlyList<UserFavorite>> GetFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<IReadOnlyList<UserFavorite>>(Array.Empty<UserFavorite>());
        }

        if (!_store.TryGetValue(userId, out var set))
        {
            return Task.FromResult<IReadOnlyList<UserFavorite>>(Array.Empty<UserFavorite>());
        }

        var favorites = set
            .Select(resortId => new UserFavorite { UserId = userId, ResortId = resortId })
            .ToArray();

        return Task.FromResult<IReadOnlyList<UserFavorite>>(favorites);
    }

    public Task AddFavoriteAsync(
        UserFavorite favorite,
        CancellationToken cancellationToken = default)
    {
        if (favorite is null)
        {
            throw new ArgumentNullException(nameof(favorite));
        }

        if (string.IsNullOrWhiteSpace(favorite.UserId))
        {
            throw new ArgumentException("UserId must be provided.", nameof(favorite));
        }

        _store.AddOrUpdate(
            favorite.UserId,
            _ => new HashSet<Guid> { favorite.ResortId },
            (_, existing) =>
            {
                existing.Add(favorite.ResortId);
                return existing;
            });

        return Task.CompletedTask;
    }

    public Task RemoveFavoriteAsync(
        string userId,
        Guid resortId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.CompletedTask;
        }

        if (_store.TryGetValue(userId, out var set))
        {
            set.Remove(resortId);
            if (set.Count == 0)
            {
                _store.TryRemove(userId, out _);
            }
        }

        return Task.CompletedTask;
    }
}

