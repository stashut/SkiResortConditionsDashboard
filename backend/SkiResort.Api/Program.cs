using Microsoft.EntityFrameworkCore;
using SkiResort.Infrastructure.Data;
using SkiResort.Infrastructure.Reports;

var builder = WebApplication.CreateBuilder(args);

// Criterion 5: Configure EF Core with PostgreSQL for RDS
builder.Services.AddDbContext<SkiResortDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
                           ?? "Host=localhost;Port=5432;Database=ski_resort;Username=ski;Password=ski_dev";
    options.UseNpgsql(connectionString);
});

// Criterion 11: Dapper-based snow comparison report repository
builder.Services.AddScoped<ISnowComparisonReportRepository>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Postgres")
                         ?? "Host=localhost;Port=5432;Database=ski_resort;Username=ski;Password=ski_dev";
    return new SnowComparisonReportRepository(connectionString);
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => "Ski Resort Conditions API");

app.MapControllers();

app.Run();

