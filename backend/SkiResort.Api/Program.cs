using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
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

// Favorites: DynamoDB in AWS (ECS task role / default credential chain). In-memory only for typical
// local `dotnet run` — ECS Fargate usually has NO static AWS_ACCESS_KEY_ID, so the old key check
// wrongly forced in-memory and favorites were lost on every task restart.
var useInMemoryFavorites = builder.Configuration.GetValue(
    "Favorites:UseInMemoryRepository",
    builder.Environment.IsDevelopment());

if (useInMemoryFavorites)
{
    builder.Services.AddSingleton<IUserFavoritesRepository, InMemoryUserFavoritesRepository>();
}
else
{
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    var tableName = builder.Configuration["Favorites:DynamoDbTableName"] ?? "SkiResortUserFavorites";
    builder.Services.AddSingleton<IUserFavoritesRepository>(_ =>
        new DynamoDbUserFavoritesRepository(
            _.GetRequiredService<IAmazonDynamoDB>(), tableName));
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

// Multi-instance (e.g. ECS desired count > 1): negotiate/SSE/POST must share state across tasks.
// Set ConnectionStrings__Redis (ElastiCache primary endpoint) in production; omit for single-instance / local dev.
var signalRBuilder = builder.Services.AddSignalR();
var redisForSignalR = builder.Configuration.GetConnectionString("Redis")
                      ?? builder.Configuration["SignalR:RedisConnectionString"];
if (!string.IsNullOrWhiteSpace(redisForSignalR))
{
    // Visible in ECS CloudWatch: confirms task definition env ConnectionStrings__Redis reached the process.
    Console.WriteLine("[SignalR] Redis backplane enabled (ConnectionStrings:Redis is set).");
    signalRBuilder.AddStackExchangeRedis(redisForSignalR, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("ski-resort-signalr");
    });
}
else
{
    Console.WriteLine(
        "[SignalR] Redis backplane NOT configured — set env ConnectionStrings__Redis for multi-task SignalR.");
}

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

// AllowCredentials: SignalR’s default client sends credentialed negotiate (XHR/fetch include). Browsers require
// Access-Control-Allow-Credentials: true on preflight + response. Origins are reflected (not *), which is valid with credentials.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()));

builder.Services.AddControllers();

// Behind ALB / API Gateway: correct scheme/host for redirects and SignalR negotiate URLs.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    // ALB is the only trusted proxy in this path; avoid rejecting forwarded headers.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
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
app.MapHub<ResortConditionsHub>("/hubs/resort-conditions", options =>
{
    // API Gateway HTTP API enforces a 29-second integration timeout.
    // Constrain the long-poll to 20 seconds so the server closes it with 204 cleanly
    // before the gateway kills it with 503 Service Unavailable.
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(20);
});

app.Run();
