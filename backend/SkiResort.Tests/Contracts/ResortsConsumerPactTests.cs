using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PactNet;
using PactNet.Matchers;
using Xunit;

namespace SkiResort.Tests.Contracts;

public sealed class ResortsConsumerPactTests
{
    private readonly IPactV4 _pact = Pact.V4(
        consumer: "SkiResortDashboardFrontend",
        provider: "SkiResortApi",
        new PactConfig
        {
            PactDir = "../../../../../pacts",
            DefaultJsonSettings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        });

    [Fact]
    public async Task GetResorts_ReturnsExpectedContract()
    {
        var pactBuilder = _pact.WithHttpInteractions();

        pactBuilder
            .UponReceiving("A request for all ski resorts")
            .WithRequest(HttpMethod.Get, "/api/resorts")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(
                Match.MinType(new
                {
                    id = Match.Type(Guid.NewGuid()),
                    name = Match.Type("Alpine Peak"),
                    region = Match.Type("Rockies"),
                    country = Match.Type("USA"),
                    elevationBaseMeters = Match.Type(1200),
                    elevationTopMeters = Match.Type(3000)
                }, 1));

        await pactBuilder.VerifyAsync(async context =>
        {
            using var client = new HttpClient { BaseAddress = context.MockServerUri };

            var resorts = await client.GetFromJsonAsync<List<ResortContract>>("/api/resorts");

            resorts.Should().NotBeNullOrEmpty();
            resorts![0].Name.Should().NotBeNullOrWhiteSpace();
        });
    }

    public sealed class ResortContract
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public int ElevationBaseMeters { get; init; }
        public int ElevationTopMeters { get; init; }
    }
}
