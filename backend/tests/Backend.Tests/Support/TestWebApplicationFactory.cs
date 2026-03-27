using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests.Support;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"backend-tests-{Guid.NewGuid():N}";
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
                services.RemoveAll<ILeaderboardRepository>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>(
                    options => options.UseInMemoryDatabase(_databaseName, _databaseRoot)
                );
                services.AddScoped<ILeaderboardRepository, PublicTestLeaderboardRepository>();
            }
        );
    }

    private sealed class PublicTestLeaderboardRepository : ILeaderboardRepository
    {
        public Task<IReadOnlyList<LeaderboardTeam>> GetTeamsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<LeaderboardTeam> teams =
            [
                new() { Id = "team-a", Name = "Team A", ColorHex = "#ff0000" },
                new() { Id = "team-b", Name = "Team B", ColorHex = "#00ff00" }
            ];

            return Task.FromResult(teams);
        }
    }
}
