using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SkiResort.Domain.Entities;
using SkiResort.Infrastructure.Data;

namespace Pact.Provider.Tests.TestInfrastructure;

public sealed class ProviderApiFactory : WebApplicationFactory<Program>
{
    private static readonly InMemoryDatabaseRoot DatabaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<SkiResortDbContext>();
            services.RemoveAll(typeof(DbContextOptions<SkiResortDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<SkiResortDbContext>));

            services.AddDbContext<SkiResortDbContext>(options =>
            {
                options.UseInMemoryDatabase("PactProviderTests", DatabaseRoot);
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SkiResortDbContext>();
            db.Database.EnsureCreated();

            SeedData(db);
        });
    }

    private static void SeedData(SkiResortDbContext db)
    {
        if (db.Resorts.Any())
        {
            return;
        }

        db.Resorts.Add(new Resort
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Alpine Peak",
            Region = "Rockies",
            Country = "USA",
            ElevationBaseMeters = 1200,
            ElevationTopMeters = 3000
        });

        db.SaveChanges();
    }
}
