using System.Net;
using System.Net.Http.Json;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using backend.Infrastructure.Auth;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Unit.Auth;

public sealed class TwitchLoginServiceResponseValidationTests
{
    [Fact]
    public async Task AuthenticateAsync_WhenBotCreatedPrincipalExistsReusesItForFirstLogin()
    {
        await using var dbContext = CreateDbContext();
        var expectedTimestamp = new DateTimeOffset(2026, 9, 8, 12, 30, 0, TimeSpan.Zero);
        var existingUserId = Guid.NewGuid();
        dbContext.Users.Add(
            new User
            {
                Id = existingUserId,
                TwitchUserId = "987654",
                Login = "old_login",
                DisplayName = "Old Name",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            }
        );
        await dbContext.SaveChangesAsync();
        using var handler = CreateIdentityHandler(
            "987654",
            "current_login",
            "Current Name",
            profileImageUrl: "https://static-cdn.jtvnw.net/user.png"
        );
        using var httpClient = new HttpClient(handler);
        var service = CreateService(
            httpClient,
            dbContext,
            new FixedTimeProvider(expectedTimestamp)
        );

        var result = await service.AuthenticateAsync("code", CancellationToken.None);

        Assert.Equal(existingUserId, result.UserId);
        Assert.False(result.IsNewUser);
        var persistedUser = await dbContext.Users.SingleAsync();
        Assert.Equal("current_login", persistedUser.Login);
        Assert.Equal("Current Name", persistedUser.DisplayName);
        Assert.Equal(expectedTimestamp.UtcDateTime, persistedUser.LastLoginAtUtc);
        Assert.Equal(expectedTimestamp.UtcDateTime, persistedUser.UpdatedAtUtc);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenAccessTokenIsEmptyRejectsResponseBeforeUserRequest()
    {
        await using var dbContext = CreateDbContext();
        using var handler = new StubHttpMessageHandler(
            [CreateJsonResponse(new { access_token = string.Empty })]
        );
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AuthenticateAsync("code", CancellationToken.None)
        );

        Assert.Equal(AppMessages.Exceptions.TwitchTokenResponseInvalid, exception.Message);
        Assert.Equal(1, handler.RequestCount);
        Assert.Empty(dbContext.Users);
    }

    [Theory]
    [InlineData("", "viewer", "Viewer")]
    [InlineData("not-numeric", "viewer", "Viewer")]
    [InlineData("123456", "", "Viewer")]
    [InlineData("123456", "viewer", "")]
    public async Task AuthenticateAsync_WhenRequiredIdentityFieldIsInvalidRejectsResponse(
        string twitchUserId,
        string login,
        string displayName
    )
    {
        await using var dbContext = CreateDbContext();
        using var handler = CreateIdentityHandler(
            twitchUserId,
            login,
            displayName,
            profileImageUrl: "https://static-cdn.jtvnw.net/user.png"
        );
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AuthenticateAsync("code", CancellationToken.None)
        );

        Assert.Equal(AppMessages.Exceptions.TwitchUsersResponseInvalid, exception.Message);
        Assert.Empty(dbContext.Users);
    }

    [Theory]
    [InlineData("http://static-cdn.jtvnw.net/user.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:password@static-cdn.jtvnw.net/user.png")]
    public async Task AuthenticateAsync_WhenProfileImageUrlIsUnsafeRejectsResponse(
        string profileImageUrl
    )
    {
        await using var dbContext = CreateDbContext();
        using var handler = CreateIdentityHandler(
            "123456",
            "viewer",
            "Viewer",
            profileImageUrl
        );
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient, dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AuthenticateAsync("code", CancellationToken.None)
        );

        Assert.Equal(AppMessages.Exceptions.TwitchUsersResponseInvalid, exception.Message);
        Assert.Empty(dbContext.Users);
    }

    private static TwitchLoginService CreateService(
        HttpClient httpClient,
        ApplicationDbContext dbContext,
        TimeProvider? timeProvider = null
    )
    {
        return new TwitchLoginService(
            httpClient,
            Options.Create(
                new TwitchAuthOptions
                {
                    ClientId = "client-id",
                    ClientSecret = "client-secret-12345",
                    RedirectUri = "https://api.example.com/auth/twitch/callback",
                    FrontendRedirectUri = "https://app.example.com/auth/callback",
                    Scopes = ["openid"]
                }
            ),
            dbContext,
            new StubUserRoleService(),
            timeProvider ?? TimeProvider.System,
            NullLogger<TwitchLoginService>.Instance
        );
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private static StubHttpMessageHandler CreateIdentityHandler(
        string twitchUserId,
        string login,
        string displayName,
        string? profileImageUrl
    )
    {
        return new StubHttpMessageHandler(
            [
                CreateJsonResponse(new { access_token = "token-123" }),
                CreateJsonResponse(
                    new
                    {
                        data = new[]
                        {
                            new
                            {
                                id = twitchUserId,
                                login,
                                display_name = displayName,
                                email = "viewer@example.com",
                                profile_image_url = profileImageUrl,
                                broadcaster_type = string.Empty,
                                type = string.Empty
                            }
                        }
                    }
                )
            ]
        );
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"twitch-response-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class StubUserRoleService : IUserRoleService
    {
        public Task<string[]> GetEffectiveRolesAsync(
            Guid userId,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult<string[]>([AuthRoleCodes.Viewer]);
        }

        public Task<string[]> EnsureEffectiveRolesAsync(
            Guid userId,
            CancellationToken cancellationToken
        )
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

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestCount++;
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No stub response configured.");
            }

            return Task.FromResult(_responses.Dequeue());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (_responses.TryDequeue(out var response))
                {
                    response.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
