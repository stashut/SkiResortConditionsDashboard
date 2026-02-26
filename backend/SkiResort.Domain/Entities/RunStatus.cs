namespace SkiResort.Domain.Entities;

// Criterion 5: Run open/closed status snapshots in RDS PostgreSQL
public class RunStatus
{
    public Guid Id { get; set; }
    public Guid ResortId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

