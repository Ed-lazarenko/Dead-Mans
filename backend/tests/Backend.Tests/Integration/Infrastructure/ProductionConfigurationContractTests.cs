using System.Net;
using Backend.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.Integration.Infrastructure;

public sealed class ProductionConfigurationContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ProductionConfigurationContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProductionConfiguration_WhenSecureAndExplicit_StartsSuccessfully()
    {
        using var factory = CreateProductionFactory(new Dictionary<string, string?>());
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://api.example.com")
            }
        );

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("Cors:AllowedOrigins:0", "http://app.example.com", "must use HTTPS")]
    [InlineData("TwitchAuth:RedirectUri", "http://api.example.com/auth/twitch/callback", "must use HTTPS")]
    [InlineData("Storage:PublicBaseUrl", "http://media.example.com", "must use HTTPS")]
    [InlineData("Storage:SecretKey", "", "access and secret keys are required")]
    [InlineData("ForwardedHeaders:TrustedProxies:0", null, "requires at least one trusted proxy")]
    [InlineData("ConnectionStrings:DefaultConnection", "Host=db.example.com;Database=deadmans;Username=deadmans;Password=test;SSL Mode=Require", "SSL Mode=VerifyFull")]
    [InlineData("AllowedHosts", "*", "must not contain wildcard")]
    [InlineData("AllowedHosts", "localhost", "must not contain localhost")]
    [InlineData("DataProtection:KeysDirectory", "", "is required in Production")]
    public void ProductionConfiguration_WhenSecurityBoundaryIsWeak_FailsAtStartup(
        string key,
        string? value,
        string expectedMessage
    )
    {
        using var factory = CreateProductionFactory(
            new Dictionary<string, string?> { [key] = value }
        );

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(expectedMessage, exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private WebApplicationFactory<Program> CreateProductionFactory(
        IReadOnlyDictionary<string, string?> overrides
    )
    {
        var settings = new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://app.example.com",
            ["Cors:AllowedOrigins:1"] = "https://admin.example.com",
            ["TwitchAuth:RedirectUri"] = "https://api.example.com/auth/twitch/callback",
            ["TwitchAuth:FrontendRedirectUri"] = "https://app.example.com/auth/callback",
            ["Storage:PublicBaseUrl"] = "https://media.example.com",
            ["Storage:ServiceUrl"] = "https://s3.example.com",
            ["Storage:BucketName"] = "deadman-test",
            ["Storage:GamesPrefix"] = "games",
            ["Storage:CardsGroup"] = "cards",
            ["Storage:AccessKey"] = "test-access-key",
            ["Storage:SecretKey"] = "test-secret-key",
            ["ConnectionStrings:DefaultConnection"] =
                "Host=db.example.com;Database=deadmans;Username=deadmans;Password=test;SSL Mode=VerifyFull",
            ["AllowedHosts"] = "api.example.com",
            ["DataProtection:KeysDirectory"] = Path.Combine(
                Path.GetTempPath(),
                "deadmans-tests",
                "data-protection-keys"
            ),
            ["ForwardedHeaders:Enabled"] = "true",
            ["ForwardedHeaders:TrustedProxies:0"] = "127.0.0.1",
            ["ForwardedHeaders:TrustAllProxiesInDevelopment"] = "false"
        };
        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration(
                (_, configuration) => configuration.AddInMemoryCollection(settings)
            );
        });
    }
}
