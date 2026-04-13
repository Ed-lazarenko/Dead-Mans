using System.Net;
using Backend.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.Integration.Infrastructure;

public sealed class ForwardedHeadersConfigurationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ForwardedHeadersConfigurationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthEndpoint_WithValidTrustedNetworksConfig_ReturnsUnauthorizedInsteadOfCrashing()
    {
        using var client = CreateClient(
            new Dictionary<string, string?>
            {
                ["ForwardedHeaders:TrustedNetworks:0"] = "10.0.0.0/24",
                ["ForwardedHeaders:TrustedProxies:0"] = "127.0.0.1",
                ["ForwardedHeaders:TrustAllProxiesInDevelopment"] = "false"
            }
        );

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void CreateClient_WithInvalidTrustedNetworkCidr_ThrowsConfigurationError()
    {
        var exception = Assert.ThrowsAny<Exception>(
            () =>
            {
                using var client = CreateClient(
                    new Dictionary<string, string?>
                    {
                        ["ForwardedHeaders:TrustedNetworks:0"] = "10.0.0.0/not-a-prefix",
                        ["ForwardedHeaders:TrustAllProxiesInDevelopment"] = "false"
                    }
                );
            }
        );

        Assert.Contains("ForwardedHeaders:TrustedNetworks", exception.ToString());
    }

    [Fact]
    public async Task AuthEndpoint_WithForwardedHeadersDisabled_IgnoresInvalidNetworkConfig()
    {
        using var client = CreateClient(
            new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "false",
                ["ForwardedHeaders:TrustedNetworks:0"] = "10.0.0.0/not-a-prefix"
            }
        );

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClient(IReadOnlyDictionary<string, string?> overrides)
    {
        var customizedFactory = _factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureAppConfiguration(
                    (_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(overrides)
                )
        );

        return customizedFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );
    }
}
