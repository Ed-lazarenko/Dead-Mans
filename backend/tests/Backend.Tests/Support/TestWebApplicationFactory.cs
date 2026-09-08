using backend.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.Support;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"backend-tests-{Guid.NewGuid():N}";
    private readonly string _dataProtectionKeysDirectory = Path.Combine(
        Path.GetTempPath(),
        "deadmans-tests",
        $"data-protection-{Guid.NewGuid():N}"
    );

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dataProtectionKeysDirectory);
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
                        ["DataProtection:KeysDirectory"] = _dataProtectionKeysDirectory,
                        ["Storage:PublicBaseUrl"] = "http://localhost:9000",
                        ["Storage:BucketName"] = "deadman-test",
                        ["Storage:GamesPrefix"] = "games",
                        ["Storage:CardsGroup"] = "cards"
                    }
                );
            }
        );
        builder.ConfigureTestServices(
            services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<IDataProtectionProvider>();
                services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
                services.AddDbContext<ApplicationDbContext>(
                    options => options.UseInMemoryDatabase(_databaseName)
                );
            }
        );
    }

    public void ResetDatabase()
    {
        using var scope = Services.CreateScope();
        var dataProtectionProvider = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        if (dataProtectionProvider is not EphemeralDataProtectionProvider)
        {
            throw new InvalidOperationException(
                $"Expected ephemeral data protection, but resolved {dataProtectionProvider.GetType().FullName}."
            );
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_dataProtectionKeysDirectory))
        {
            Directory.Delete(_dataProtectionKeysDirectory, recursive: true);
        }
    }
}
