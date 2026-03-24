using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using backend.Api.Contracts;
using backend.Api.Mapping;
using backend.Application.Abstractions.Auth;
using backend.Application.Features.Auth;
using backend.Data;
using backend.Data.Entities;
using backend.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Backend.Tests;

public sealed class AuthSessionConsistencyTests
{
    [Fact]
    public async Task GetSessionAsync_DoesNotPersistViewerRoleOnRead()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = "twitch-user-1",
                Login = "viewer1",
                DisplayName = "Viewer One",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        dbContext.Roles.Add(
            new Role
            {
                Id = 1,
                Code = "viewer",
                Name = "Viewer",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();

        var authUserReader = new DbAuthUserReader(dbContext);
        var roleService = new UserRoleService(dbContext);
        var sessionService = new AuthSessionService(authUserReader, roleService);

        var session = await sessionService.GetSessionAsync(userId, CancellationToken.None);

        Assert.NotNull(session);
        Assert.Equal(["viewer"], session.Roles);
        Assert.Empty(await dbContext.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task GetSessionAsync_WhenUserInactive_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = "inactive-user-session",
                Login = "inactive-session",
                DisplayName = "Inactive Session User",
                IsActive = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();

        var authUserReader = new DbAuthUserReader(dbContext);
        var roleService = new UserRoleService(dbContext);
        var sessionService = new AuthSessionService(authUserReader, roleService);

        var session = await sessionService.GetSessionAsync(userId, CancellationToken.None);

        Assert.Null(session);
    }

    [Fact]
    public void ToDto_FiltersUnsupportedRoles()
    {
        var session = new AuthSession(
            Guid.NewGuid(),
            "Test User",
            ["viewer", "experimental", "moderator"]
        );

        var dto = session.ToDto();

        Assert.Equal([AuthRole.Viewer, AuthRole.Moderator], dto.Roles);
    }

    [Fact]
    public async Task TransformAsync_ReplacesCookieRoleClaimsWithCurrentDatabaseRoles()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = "twitch-user-2",
                Login = "moderator1",
                DisplayName = "Moderator One",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        dbContext.Roles.AddRange(
            new Role
            {
                Id = 1,
                Code = "viewer",
                Name = "Viewer",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new Role
            {
                Id = 2,
                Code = "moderator",
                Name = "Moderator",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        dbContext.UserRoles.Add(
            new UserRole
            {
                UserId = userId,
                RoleId = 2,
                AssignedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();

        var transformer = new CurrentUserRoleClaimsTransformation(
            new DbAuthUserReader(dbContext),
            new UserRoleService(dbContext)
        );
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, "Moderator One"),
                    new Claim(ClaimTypes.Role, "admin")
                ],
                "test"
            )
        );

        var transformed = await transformer.TransformAsync(principal);
        var roleClaims = transformed.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();

        Assert.Equal(["viewer", "moderator"], roleClaims);
    }

    [Fact]
    public async Task TransformAsync_WhenUserMissing_RemovesStaleRoleClaims()
    {
        await using var dbContext = CreateDbContext();
        var transformer = new CurrentUserRoleClaimsTransformation(
            new DbAuthUserReader(dbContext),
            new UserRoleService(dbContext)
        );
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "admin")
                ],
                "test"
            )
        );

        var transformed = await transformer.TransformAsync(principal);

        Assert.Empty(transformed.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserIsInactive_ThrowsInactiveUserLoginException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(
            new User
            {
                Id = Guid.NewGuid(),
                TwitchUserId = "inactive-twitch-user",
                Login = "inactive-user",
                DisplayName = "Inactive User",
                IsActive = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();

        var httpClient = new HttpClient(
            new StubHttpMessageHandler(
                [
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new { access_token = "token-123" })
                    },
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(
                            new
                            {
                                data = new[]
                                {
                                    new
                                    {
                                        id = "inactive-twitch-user",
                                        login = "inactive-user",
                                        display_name = "Inactive User",
                                        email = "inactive@example.com",
                                        profile_image_url = (string?)null,
                                        broadcaster_type = (string?)null,
                                        type = (string?)null
                                    }
                                }
                            }
                        )
                    }
                ]
            )
        );

        var service = new TwitchLoginService(
            httpClient,
            Options.Create(
                new TwitchAuthOptions
                {
                    ClientId = "client-id",
                    ClientSecret = "client-secret-12345",
                    RedirectUri = "https://example.com/auth/twitch/callback",
                    FrontendRedirectUri = "https://example.com/auth/callback",
                    Scopes = ["openid"]
                }
            ),
            dbContext,
            new StubUserRoleService()
        );

        await Assert.ThrowsAsync<InactiveUserLoginException>(
            () => service.AuthenticateAsync("code-123", CancellationToken.None)
        );
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"auth-session-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class StubUserRoleService : IUserRoleService
    {
        public Task<string[]> GetEffectiveRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<string[]>([AuthRoleCodes.Viewer]);
        }

        public Task<string[]> EnsureEffectiveRolesAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<string[]>([AuthRoleCodes.Viewer]);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public StubHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No stub response configured for HTTP call.");
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
