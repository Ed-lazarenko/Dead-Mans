using backend.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.Integration;

public sealed class AuthorizedTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"backend-authz-tests-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

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
                    options => options
                        .UseInMemoryDatabase(_databaseName, _databaseRoot)
                        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                );

                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { }
                    );
            }
        );
    }
}
