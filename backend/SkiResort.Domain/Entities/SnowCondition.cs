namespace SkiResort.Domain.Entities;

// Criterion 5: Historical snow depth records in RDS PostgreSQL
public class SnowCondition
{
    public Guid Id { get; set; }
    public Guid ResortId { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public decimal SnowDepthCm { get; set; }
    public decimal NewSnowCm { get; set; }
}

