using System;
using System.Collections.Generic;

namespace SkiResort.Api.Models;

public sealed record ResortDto(
    Guid Id,
    string Name,
    string Region,
    string Country,
    int ElevationBaseMeters,
    int ElevationTopMeters);

public sealed record SnowConditionDto(
    Guid Id,
    Guid ResortId,
    DateTimeOffset ObservedAt,
    decimal SnowDepthCm,
    decimal NewSnowCm);

public sealed record LiftStatusDto(
    Guid Id,
    Guid ResortId,
    string Name,
    bool IsOpen,
    DateTimeOffset UpdatedAt);

public sealed record RunStatusDto(
    Guid Id,
    Guid ResortId,
    string Name,
    bool IsOpen,
    DateTimeOffset UpdatedAt);

public sealed class RunStatusPageDto
{
    public IReadOnlyList<RunStatusDto> Items { get; init; } = Array.Empty<RunStatusDto>();
    public DateTimeOffset? NextUpdatedBefore { get; init; }
}

public sealed class ResortConditionsResponse
{
    public required ResortDto Resort { get; init; }
    public SnowConditionDto? LatestSnowCondition { get; init; }
    public IReadOnlyList<LiftStatusDto> CurrentLiftStatuses { get; init; } = Array.Empty<LiftStatusDto>();
    public RunStatusPageDto RunStatusPage { get; init; } = new();
}

