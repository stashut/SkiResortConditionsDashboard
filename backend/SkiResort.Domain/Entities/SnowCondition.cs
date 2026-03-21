namespace SkiResort.Domain.Entities;

// Criterion 5: Historical snow / weather records in RDS PostgreSQL
public class SnowCondition
{
    public Guid Id { get; set; }
    public Guid ResortId { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public decimal SnowDepthCm { get; set; }
    public decimal NewSnowCm { get; set; }

    // Open-Meteo hourly snapshot (same UTC hour as ObservedAt)
    public decimal? TemperatureCelsius { get; set; }
    public decimal? ApparentTemperatureCelsius { get; set; }
    public decimal? RelativeHumidityPercent { get; set; }
    public decimal? PrecipitationMm { get; set; }
    public decimal? RainMm { get; set; }
    public int? WeatherCode { get; set; }
    public decimal? CloudCoverPercent { get; set; }
    public decimal? WindSpeedKmh { get; set; }
    public int? WindDirectionDeg { get; set; }
    public decimal? WindGustsKmh { get; set; }
    public int? VisibilityMeters { get; set; }
    public decimal? SurfacePressureHpa { get; set; }
}
