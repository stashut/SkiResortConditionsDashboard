namespace SkiResort.Domain.Entities;

// Criterion 5: Stored in RDS PostgreSQL via EF Core
public class Resort
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int ElevationBaseMeters { get; set; }
    public int ElevationTopMeters { get; set; }
}
