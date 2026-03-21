using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SkiResort.Api.Hubs;
using SkiResort.Api.Observability;
using SkiResort.Api.Options;
using SkiResort.Api.Realtime;
using SkiResort.Api.Workers;
using SkiResort.Domain.Entities;
using SkiResort.Infrastructure.Data;
using SkiResort.Infrastructure.Favorites;
using SkiResort.Infrastructure.Reports;

var builder = WebApplication.CreateBuilder(args);

// Configure AWS SDK default options (region, credentials, etc.)
var awsOptions = builder.Configuration.GetAWSOptions();
awsOptions.Region ??= RegionEndpoint.USEast1;
builder.Services.AddDefaultAWSOptions(awsOptions);

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

// Criterion 4 & 10: DynamoDB-backed user favorites (cloud-synced state)
// In local/dev we often don't have AWS credentials, so fall back to an in-memory implementation.
var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey))
{
    builder.Services.AddSingleton<IUserFavoritesRepository, InMemoryUserFavoritesRepository>();
}
else
{
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddSingleton<IUserFavoritesRepository>(_ =>
        new DynamoDbUserFavoritesRepository(
            _.GetRequiredService<IAmazonDynamoDB>(),
            "SkiResortUserFavorites"));
}

// Criterion 3: SQS-based weather ingestion worker
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.Configure<SqsIngestionOptions>(builder.Configuration.GetSection("SqsIngestion"));
builder.Services.AddHostedService<SqsIngestionWorker>();

// Criterion 9: SignalR hub and update notifier for real-time resort conditions
builder.Services.AddSignalR();
builder.Services.AddSingleton<ResortUpdateNotifier>();

// Session middleware (available for request-scoped state)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".SkiResort.Session";
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

// Criterion 8: OpenTelemetry instrumentation exported via OTLP
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("SkiResort.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource(ObservabilityConstants.ActivitySourceName)
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(ObservabilityConstants.MeterName)
            .AddOtlpExporter();
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)   // any origin; tighten once a stable domain is set
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();             // required for SignalR WebSocket negotiation
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors();
app.UseSession();

// Dev-only: create schema + seed sample data if the DB is empty.
// This avoids "relation ... does not exist" when running locally without migrations.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SkiResortDbContext>();

    db.Database.EnsureCreated();

    if (!db.Resorts.Any())
    {
        var now = DateTimeOffset.UtcNow;

        // Stable IDs so the UI can reference same objects after restarts.
        var resortA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var resortB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        db.Resorts.AddRange(
            new Resort
            {
                Id = resortA,
                Name = "North Peak",
                Region = "North",
                Country = "US",
                ElevationBaseMeters = 420,
                ElevationTopMeters = 1650
            },
            new Resort
            {
                Id = resortB,
                Name = "Alpine Ridge",
                Region = "Central",
                Country = "US",
                ElevationBaseMeters = 560,
                ElevationTopMeters = 2140
            });

        db.SnowConditions.AddRange(
            new SnowCondition
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ResortId = resortA,
                ObservedAt = now.AddHours(-1),
                SnowDepthCm = 85,
                NewSnowCm = 12
            },
            new SnowCondition
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ResortId = resortB,
                ObservedAt = now.AddHours(-1),
                SnowDepthCm = 72,
                NewSnowCm = 9
            });

        db.LiftStatuses.AddRange(
            new LiftStatus
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                ResortId = resortA,
                Name = "Lift 1",
                IsOpen = true,
                UpdatedAt = now.AddMinutes(-5)
            },
            new LiftStatus
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                ResortId = resortB,
                Name = "Lift A",
                IsOpen = false,
                UpdatedAt = now.AddMinutes(-5)
            });

        db.RunStatuses.AddRange(
            new RunStatus
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                ResortId = resortA,
                Name = "Green Trail",
                IsOpen = true,
                UpdatedAt = now.AddMinutes(-3)
            },
            new RunStatus
            {
                Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                ResortId = resortB,
                Name = "Blue Line",
                IsOpen = true,
                UpdatedAt = now.AddMinutes(-3)
            });

        db.SaveChanges();
    }
}

app.MapGet("/", () => "Ski Resort Conditions API");

app.MapControllers();

// SignalR hub for live resort condition updates
app.MapHub<ResortConditionsHub>("/hubs/resort-conditions");

app.Run();

