using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Realtime;
using SkiResort.Domain.Entities;
using SkiResort.Infrastructure.Data;

namespace SkiResort.Api.Workers;

/// <summary>
/// Fetches hourly weather from Open-Meteo (https://open-meteo.com) for every resort
/// with coordinates. Persists snow + temperature, wind, precipitation, etc. to RDS
/// and notifies SignalR clients. No API key required.
/// </summary>
public sealed class WeatherSyncWorker : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ResortUpdateNotifier _notifier;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherSyncWorker> _logger;

    public WeatherSyncWorker(
        IServiceScopeFactory scopeFactory,
        ResortUpdateNotifier notifier,
        IHttpClientFactory httpClientFactory,
        ILogger<WeatherSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncAllResortsAsync(stoppingToken);
            await Task.Delay(SyncInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task SyncAllResortsAsync(CancellationToken ct)
    {
        _logger.LogInformation("WeatherSyncWorker: starting hourly sync");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SkiResortDbContext>();

        var resorts = await db.Resorts
            .Where(r => r.LatitudeDeg != null && r.LongitudeDeg != null)
            .ToListAsync(ct);

        _logger.LogInformation("WeatherSyncWorker: syncing {Count} resorts", resorts.Count);

        foreach (var resort in resorts)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await SyncResortAsync(db, resort, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "WeatherSyncWorker: failed to sync resort {Name} ({Id})",
                    resort.Name, resort.Id);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), ct).ConfigureAwait(false);
        }

        _logger.LogInformation("WeatherSyncWorker: sync complete");
    }

    private async Task SyncResortAsync(SkiResortDbContext db, Resort resort, CancellationToken ct)
    {
        var snap = await FetchOpenMeteoSnapshotAsync(
            resort.LatitudeDeg!.Value, resort.LongitudeDeg!.Value, ct);

        if (snap is null || (!snap.SnowDepthCm.HasValue && !snap.TemperatureCelsius.HasValue))
        {
            _logger.LogDebug("WeatherSyncWorker: no usable data for {Name}", resort.Name);
            return;
        }

        var condition = new SnowCondition
        {
            Id = Guid.NewGuid(),
            ResortId = resort.Id,
            ObservedAt = DateTimeOffset.UtcNow,
            SnowDepthCm = snap.SnowDepthCm ?? 0,
            NewSnowCm = snap.NewSnowCm ?? 0,
            TemperatureCelsius = snap.TemperatureCelsius,
            ApparentTemperatureCelsius = snap.ApparentTemperatureCelsius,
            RelativeHumidityPercent = snap.RelativeHumidityPercent,
            PrecipitationMm = snap.PrecipitationMm,
            RainMm = snap.RainMm,
            WeatherCode = snap.WeatherCode,
            CloudCoverPercent = snap.CloudCoverPercent,
            WindSpeedKmh = snap.WindSpeedKmh,
            WindDirectionDeg = snap.WindDirectionDeg,
            WindGustsKmh = snap.WindGustsKmh,
            VisibilityMeters = snap.VisibilityMeters,
            SurfacePressureHpa = snap.SurfacePressureHpa
        };

        db.SnowConditions.Add(condition);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "WeatherSyncWorker: {Name} — depth {Depth} cm, new {New} cm, temp {Temp} °C",
            resort.Name, condition.SnowDepthCm, condition.NewSnowCm,
            condition.TemperatureCelsius?.ToString() ?? "—");

        await _notifier.NotifyResortConditionsUpdatedAsync(resort.Id);
    }

    private async Task<OpenMeteoSnapshot?> FetchOpenMeteoSnapshotAsync(
        double lat, double lon, CancellationToken ct)
    {
        const string hourly =
            "snow_depth,snowfall,temperature_2m,apparent_temperature,relative_humidity_2m," +
            "precipitation,rain,weather_code,cloud_cover,wind_speed_10m,wind_direction_10m," +
            "wind_gusts_10m,visibility,surface_pressure";

        var url = $"https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={lat:F4}&longitude={lon:F4}" +
                  $"&hourly={hourly}" +
                  $"&forecast_days=1&timezone=UTC&windspeed_unit=kmh";

        var http = _httpClientFactory.CreateClient("openmeteo");
        using var response = await http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("WeatherSyncWorker: Open-Meteo returned {Status} for {Lat},{Lon}",
                response.StatusCode, lat, lon);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var data = JsonSerializer.Deserialize<OpenMeteoResponse>(body, JsonOptions);

        if (data?.Hourly is null) return null;

        var nowHour = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:00");
        var idx = data.Hourly.Time.IndexOf(nowHour);
        if (idx < 0) idx = 0;

        static double? At(List<double?> list, int i) =>
            i >= 0 && i < list.Count ? list[i] : null;

        var h = data.Hourly;

        var depthM = At(h.SnowDepth, idx);
        var snowfallCm = At(h.Snowfall, idx);
        decimal? depthCm = depthM.HasValue
            ? Math.Round((decimal)(depthM.Value * 100), 1)
            : null;
        decimal? newSnow = snowfallCm.HasValue
            ? Math.Round((decimal)snowfallCm.Value, 1)
            : null;

        int? weatherCode = At(h.WeatherCode, idx) is { } wc
            ? (int)Math.Round(wc)
            : null;
        int? windDir = At(h.WindDirection10m, idx) is { } wd
            ? (int)Math.Round(wd)
            : null;
        int? visM = At(h.Visibility, idx) is { } v
            ? (int)Math.Round(v)
            : null;

        return new OpenMeteoSnapshot(
            SnowDepthCm: depthCm,
            NewSnowCm: newSnow,
            TemperatureCelsius: D(At(h.Temperature2m, idx)),
            ApparentTemperatureCelsius: D(At(h.ApparentTemperature, idx)),
            RelativeHumidityPercent: D(At(h.RelativeHumidity2m, idx)),
            PrecipitationMm: D(At(h.Precipitation, idx)),
            RainMm: D(At(h.Rain, idx)),
            WeatherCode: weatherCode,
            CloudCoverPercent: D(At(h.CloudCover, idx)),
            WindSpeedKmh: D(At(h.WindSpeed10m, idx)),
            WindDirectionDeg: windDir,
            WindGustsKmh: D(At(h.WindGusts10m, idx)),
            VisibilityMeters: visM,
            SurfacePressureHpa: D(At(h.SurfacePressure, idx))
        );
    }

    private static decimal? D(double? v) =>
        v.HasValue ? Math.Round((decimal)v.Value, 2) : null;

    private sealed record OpenMeteoSnapshot(
        decimal? SnowDepthCm,
        decimal? NewSnowCm,
        decimal? TemperatureCelsius,
        decimal? ApparentTemperatureCelsius,
        decimal? RelativeHumidityPercent,
        decimal? PrecipitationMm,
        decimal? RainMm,
        int? WeatherCode,
        decimal? CloudCoverPercent,
        decimal? WindSpeedKmh,
        int? WindDirectionDeg,
        decimal? WindGustsKmh,
        int? VisibilityMeters,
        decimal? SurfacePressureHpa);

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("hourly")]
        public HourlyData? Hourly { get; set; }
    }

    private sealed class HourlyData
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; } = [];

        [JsonPropertyName("snow_depth")]
        public List<double?> SnowDepth { get; set; } = [];

        [JsonPropertyName("snowfall")]
        public List<double?> Snowfall { get; set; } = [];

        [JsonPropertyName("temperature_2m")]
        public List<double?> Temperature2m { get; set; } = [];

        [JsonPropertyName("apparent_temperature")]
        public List<double?> ApparentTemperature { get; set; } = [];

        [JsonPropertyName("relative_humidity_2m")]
        public List<double?> RelativeHumidity2m { get; set; } = [];

        [JsonPropertyName("precipitation")]
        public List<double?> Precipitation { get; set; } = [];

        [JsonPropertyName("rain")]
        public List<double?> Rain { get; set; } = [];

        [JsonPropertyName("weather_code")]
        public List<double?> WeatherCode { get; set; } = [];

        [JsonPropertyName("cloud_cover")]
        public List<double?> CloudCover { get; set; } = [];

        [JsonPropertyName("wind_speed_10m")]
        public List<double?> WindSpeed10m { get; set; } = [];

        [JsonPropertyName("wind_direction_10m")]
        public List<double?> WindDirection10m { get; set; } = [];

        [JsonPropertyName("wind_gusts_10m")]
        public List<double?> WindGusts10m { get; set; } = [];

        [JsonPropertyName("visibility")]
        public List<double?> Visibility { get; set; } = [];

        [JsonPropertyName("surface_pressure")]
        public List<double?> SurfacePressure { get; set; } = [];
    }
}
