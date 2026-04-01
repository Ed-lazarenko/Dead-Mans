using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using Backend.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class UnavailablePublicGameFeaturesContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UnavailablePublicGameFeaturesContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLoadout_WhenBoardMissing_ReturnsServiceUnavailableWithErrorPayload()
    {
        await ClearBoardsAsync();

        var response = await _client.GetAsync("/api/loadout");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("No loadout board", error.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetGameState_WhenPersistenceUnavailable_ReturnsServiceUnavailableWithErrorPayload()
    {
        var response = await _client.GetAsync("/api/game-state");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("Configure a persistence-backed implementation", error.Error, StringComparison.OrdinalIgnoreCase);
    }

    private async Task ClearBoardsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.BoardCells.RemoveRange(db.BoardCells);
        db.GameBoards.RemoveRange(db.GameBoards);
        db.Games.RemoveRange(db.Games);
        await db.SaveChangesAsync();
    }
}

public sealed class UnavailableProtectedGameFeaturesContractTests
    : IClassFixture<AuthorizedTestWebApplicationFactory>
{
    private readonly AuthorizedTestWebApplicationFactory _factory;

    public UnavailableProtectedGameFeaturesContractTests(AuthorizedTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetModifiers_WhenAuthenticatedModeratorAndPersistenceUnavailable_ReturnsServiceUnavailable()
    {
        var userId = await SeedUserWithRolesAsync([AuthRoleCodes.Moderator]);
        using var client = CreateAuthenticatedClient(userId, staleCookieRoles: [AuthRoleCodes.Viewer]);

        var response = await client.GetAsync("/api/modifiers");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("Configure a persistence-backed implementation", error.Error, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateAuthenticatedClient(Guid userId, string[] staleCookieRoles)
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            }
        );
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-DisplayName", "Moderator User");
        if (staleCookieRoles.Length > 0)
        {
            client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(',', staleCookieRoles));
        }

        return client;
    }

    private async Task<Guid> SeedUserWithRolesAsync(string[] roleCodes)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.UserRoles.RemoveRange(dbContext.UserRoles);
        dbContext.Users.RemoveRange(dbContext.Users);
        dbContext.Roles.RemoveRange(dbContext.Roles);

        var roles = new[]
        {
            new Role
            {
                Id = 1,
                Code = AuthRoleCodes.Viewer,
                Name = "Viewer",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new Role
            {
                Id = 2,
                Code = AuthRoleCodes.Moderator,
                Name = "Moderator",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new Role
            {
                Id = 3,
                Code = AuthRoleCodes.Admin,
                Name = "Administrator",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        };
        dbContext.Roles.AddRange(roles);

        var userId = Guid.NewGuid();
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"test-user-{userId:N}",
                Login = "moderator-user",
                DisplayName = "Moderator User",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );

        var roleByCode = roles.ToDictionary(role => role.Code, StringComparer.Ordinal);
        dbContext.UserRoles.AddRange(
            roleCodes.Select(code => new UserRole
            {
                UserId = userId,
                RoleId = roleByCode[code].Id,
                AssignedAtUtc = DateTime.UtcNow
            })
        );

        await dbContext.SaveChangesAsync();
        return userId;
    }
}
