using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SkiResort.Api;
using SkiResort.Api.Hubs;
using SkiResort.Api.Observability;
using SkiResort.Api.Options;
using SkiResort.Api.Realtime;
using SkiResort.Api.Workers;
using SkiResort.Infrastructure.Data;
using SkiResort.Infrastructure.Favorites;
using SkiResort.Infrastructure.Reports;

var builder = WebApplication.CreateBuilder(args);

var awsOptions = builder.Configuration.GetAWSOptions();
awsOptions.Region ??= RegionEndpoint.USEast1;
builder.Services.AddDefaultAWSOptions(awsOptions);

builder.Services.AddDbContext<SkiResortDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres")
                           ?? "Host=localhost;Port=5432;Database=ski_resort;Username=ski;Password=ski_dev";
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ISnowComparisonReportRepository>(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
                         ?? "Host=localhost;Port=5432;Database=ski_resort;Username=ski;Password=ski_dev";
    return new SnowComparisonReportRepository(connectionString);
});

// DynamoDB-backed favorites; falls back to in-memory when AWS credentials are absent (local dev).
var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey))
    builder.Services.AddSingleton<IUserFavoritesRepository, InMemoryUserFavoritesRepository>();
else
{
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddSingleton<IUserFavoritesRepository>(_ =>
        new DynamoDbUserFavoritesRepository(
            _.GetRequiredService<IAmazonDynamoDB>(), "SkiResortUserFavorites"));
}

builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.Configure<SqsIngestionOptions>(builder.Configuration.GetSection("SqsIngestion"));
builder.Services.AddHostedService<SqsIngestionWorker>();
builder.Services.AddHostedService<WeatherSyncWorker>();

builder.Services.AddHttpClient("openmeteo", c =>
{
    c.BaseAddress = new Uri("https://api.open-meteo.com");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<ResortUpdateNotifier>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".SkiResort.Session";
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("SkiResort.Api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource(ObservabilityConstants.ActivitySourceName)
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(ObservabilityConstants.MeterName)
        .AddOtlpExporter());

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()));

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors();
app.UseSession();

// Dev-only: ensure schema exists and seed worldwide resort data if the DB is empty.
// See DevSeeder.cs for the full resort/lift/run list.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SkiResortDbContext>();
    db.Database.EnsureCreated();
    DevSeeder.Seed(db);
}

app.MapGet("/", () => "Ski Resort Conditions API");
app.MapControllers();
app.MapHub<ResortConditionsHub>("/hubs/resort-conditions");

app.Run();
