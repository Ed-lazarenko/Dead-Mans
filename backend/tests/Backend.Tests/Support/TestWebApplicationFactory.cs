using backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.Support;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"backend-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("hostBuilder:reloadConfigOnChange", "false");
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["TwitchAuth:ClientId"] = "test-client-id",
                        ["TwitchAuth:ClientSecret"] = "test-client-secret-12345",
                        ["TwitchAuth:RedirectUri"] = "https://example.com/auth/twitch/callback",
                        ["TwitchAuth:FrontendRedirectUri"] = "https://example.com/auth/callback",
                        ["TwitchAuth:Scopes:0"] = "openid",
                        ["TwitchAuth:Scopes:1"] = "user:read:email",
                        ["Storage:PublicBaseUrl"] = "http://localhost:9000",
                        ["Storage:BucketName"] = "deadman-test",
                        ["Storage:GamesPrefix"] = "games",
                        ["Storage:CardsGroup"] = "cards"
                    }
                );
            }
        );
        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>(
                    options => options.UseInMemoryDatabase(_databaseName)
                );
            }
        );
    }
}
