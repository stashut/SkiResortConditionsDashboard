using Microsoft.EntityFrameworkCore;
using SkiResort.Domain.Entities;

namespace SkiResort.Infrastructure.Data;

// Criterion 5: EF Core DbContext targeting RDS PostgreSQL
public class SkiResortDbContext : DbContext
{
    public DbSet<Resort> Resorts => Set<Resort>();
    public DbSet<SnowCondition> SnowConditions => Set<SnowCondition>();
    public DbSet<LiftStatus> LiftStatuses => Set<LiftStatus>();
    public DbSet<RunStatus> RunStatuses => Set<RunStatus>();

    public SkiResortDbContext(DbContextOptions<SkiResortDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resort>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired();
            entity.Property(r => r.Region).IsRequired();
        });

        modelBuilder.Entity<SnowCondition>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.ResortId, c.ObservedAt });
        });

        modelBuilder.Entity<LiftStatus>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => new { l.ResortId, l.UpdatedAt });
        });

        modelBuilder.Entity<RunStatus>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.ResortId, r.UpdatedAt });
        });
    }
}

