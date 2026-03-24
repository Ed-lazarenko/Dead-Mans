using backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.Integration;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = string.Empty,
                        ["TwitchAuth:ClientId"] = "test-client-id",
                        ["TwitchAuth:ClientSecret"] = "test-client-secret-12345",
                        ["TwitchAuth:RedirectUri"] = "https://example.com/auth/twitch/callback",
                        ["TwitchAuth:FrontendRedirectUri"] = "https://example.com/auth/callback",
                        ["TwitchAuth:Scopes:0"] = "openid",
                        ["TwitchAuth:Scopes:1"] = "user:read:email"
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
                    options => options.UseInMemoryDatabase($"backend-tests-{Guid.NewGuid():N}")
                );
            }
        );
    }
}
