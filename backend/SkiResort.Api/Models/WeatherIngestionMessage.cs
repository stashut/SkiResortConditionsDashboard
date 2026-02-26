using System;

namespace SkiResort.Api.Models;

/// <summary>
/// Represents a simplified weather ingestion message delivered via SQS.
/// </summary>
public sealed class WeatherIngestionMessage
{
    public Guid ResortId { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public decimal SnowDepthCm { get; set; }
    public decimal NewSnowCm { get; set; }
}

