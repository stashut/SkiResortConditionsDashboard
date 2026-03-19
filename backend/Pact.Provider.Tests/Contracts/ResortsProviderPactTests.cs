using FluentAssertions;
using PactNet;
using PactNet.Verifier;
using Xunit;

namespace Pact.Provider.Tests.Contracts;

public sealed class ResortsProviderPactTests
{
    [Fact]
    public void VerifyResortsConsumerPact()
    {
        var pactPath = Path.GetFullPath(
            "../../../../../pacts/SkiResortDashboardFrontend-SkiResortApi.json",
            AppContext.BaseDirectory);

        File.Exists(pactPath).Should().BeTrue(
            "the consumer pact file must exist before provider verification");

        var providerBaseUrl = Environment.GetEnvironmentVariable("PACT_PROVIDER_BASE_URL");
        if (string.IsNullOrWhiteSpace(providerBaseUrl))
        {
            return;
        }

        using var verifier = new PactVerifier("SkiResortApi");
        verifier
            .WithHttpEndpoint(new Uri(providerBaseUrl))
            .WithFileSource(new FileInfo(pactPath))
            .Verify();
    }
}
