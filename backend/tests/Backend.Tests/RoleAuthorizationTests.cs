using System.Net;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

public sealed class RoleAuthorizationTests : IClassFixture<AuthorizedTestWebApplicationFactory>
{
    private readonly AuthorizedTestWebApplicationFactory _factory;

    public RoleAuthorizationTests(AuthorizedTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartGame_WhenAuthenticatedViewer_ReturnsForbidden()
    {
        var userId = await SeedUserWithRolesAsync([AuthRoleCodes.Viewer]);
        using var client = CreateAuthenticatedClient(userId, staleCookieRoles: [AuthRoleCodes.Admin]);

        var response = await client.PostAsync("/api/game-state/start", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetModifiers_WhenAuthenticatedViewer_ReturnsForbidden()
    {
        var userId = await SeedUserWithRolesAsync([AuthRoleCodes.Viewer]);
        using var client = CreateAuthenticatedClient(userId, staleCookieRoles: [AuthRoleCodes.Admin]);

        var response = await client.GetAsync("/api/modifiers");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
        client.DefaultRequestHeaders.Add("X-Test-DisplayName", "Viewer User");
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
                Login = "viewer-user",
                DisplayName = "Viewer User",
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
