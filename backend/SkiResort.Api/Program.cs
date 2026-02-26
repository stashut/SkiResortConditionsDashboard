using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using SkiResort.Api.Hubs;
using SkiResort.Api.Options;
using SkiResort.Api.Realtime;
using SkiResort.Api.Workers;
using SkiResort.Infrastructure.Data;
using SkiResort.Infrastructure.Favorites;
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

// Criterion 4 & 10: DynamoDB-backed user favorites (cloud-synced state)
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddSingleton<IUserFavoritesRepository>(_ =>
    new DynamoDbUserFavoritesRepository(_.GetRequiredService<IAmazonDynamoDB>(), "SkiResortUserFavorites"));

// Criterion 3: SQS-based weather ingestion worker
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.Configure<SqsIngestionOptions>(builder.Configuration.GetSection("SqsIngestion"));
builder.Services.AddHostedService<SqsIngestionWorker>();

// Criterion 9: SignalR hub and update notifier for real-time resort conditions
builder.Services.AddSignalR();
builder.Services.AddSingleton<ResortUpdateNotifier>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => "Ski Resort Conditions API");

app.MapControllers();

// SignalR hub for live resort condition updates
app.MapHub<ResortConditionsHub>("/hubs/resort-conditions");

app.Run();

