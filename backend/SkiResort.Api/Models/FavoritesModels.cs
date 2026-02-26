using System;

namespace SkiResort.Api.Models;

public sealed record UserFavoriteDto(
    string UserId,
    Guid ResortId);

public sealed record AddFavoriteRequest(
    Guid ResortId);

