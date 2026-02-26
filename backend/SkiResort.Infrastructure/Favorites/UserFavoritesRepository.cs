using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using SkiResort.Domain.Entities;

namespace SkiResort.Infrastructure.Favorites;

public interface IUserFavoritesRepository
{
    Task<IReadOnlyList<UserFavorite>> GetFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task AddFavoriteAsync(
        UserFavorite favorite,
        CancellationToken cancellationToken = default);

    Task RemoveFavoriteAsync(
        string userId,
        Guid resortId,
        CancellationToken cancellationToken = default);
}

public sealed class DynamoDbUserFavoritesRepository : IUserFavoritesRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;

    public DynamoDbUserFavoritesRepository(IAmazonDynamoDB dynamoDb, string tableName = "SkiResortUserFavorites")
    {
        _dynamoDb = dynamoDb ?? throw new ArgumentNullException(nameof(dynamoDb));
        _tableName = string.IsNullOrWhiteSpace(tableName)
            ? throw new ArgumentException("Table name must be provided.", nameof(tableName))
            : tableName;
    }

    public async Task<IReadOnlyList<UserFavorite>> GetFavoritesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<UserFavorite>();
        }

        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "UserId = :userId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":userId"] = new AttributeValue { S = userId }
            }
        };

        var response = await _dynamoDb.QueryAsync(request, cancellationToken).ConfigureAwait(false);

        var results = new List<UserFavorite>(response.Items.Count);

        foreach (var item in response.Items)
        {
            if (!item.TryGetValue("ResortId", out var resortIdAttr) || string.IsNullOrWhiteSpace(resortIdAttr.S))
            {
                continue;
            }

            if (!Guid.TryParse(resortIdAttr.S, out var resortId))
            {
                continue;
            }

            results.Add(new UserFavorite
            {
                UserId = userId,
                ResortId = resortId
            });
        }

        return results;
    }

    public async Task AddFavoriteAsync(
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

        var item = new Dictionary<string, AttributeValue>
        {
            ["UserId"] = new AttributeValue { S = favorite.UserId },
            ["ResortId"] = new AttributeValue { S = favorite.ResortId.ToString("D") }
        };

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await _dynamoDb.PutItemAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveFavoriteAsync(
        string userId,
        Guid resortId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var key = new Dictionary<string, AttributeValue>
        {
            ["UserId"] = new AttributeValue { S = userId },
            ["ResortId"] = new AttributeValue { S = resortId.ToString("D") }
        };

        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = key
        };

        await _dynamoDb.DeleteItemAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

