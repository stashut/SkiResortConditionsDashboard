namespace SkiResort.Domain.Entities;

// Criterion 5: Lift open/closed status snapshots in RDS PostgreSQL
public class LiftStatus
{
    public Guid Id { get; set; }
    public Guid ResortId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

