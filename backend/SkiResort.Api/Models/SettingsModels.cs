namespace SkiResort.Api.Models;

public sealed record UserSettingsDto(
    string UnitPreference,
    string? RegionFilter,
    string? LastViewedResortId);
