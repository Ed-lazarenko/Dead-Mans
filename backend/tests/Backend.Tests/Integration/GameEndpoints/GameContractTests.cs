using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Realtime;
using backend.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Backend.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGame_WhenAnonymous_ReturnsJsonUnauthorizedError()
    {
        var response = await _client.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.AuthenticationRequired, payload.Error);
    }

    [Fact]
    public async Task GetGame_WhenAuthenticated_ReturnsBoardSnapshot()
    {
        var finishedGameId = await SeedGamesAsync();
        await AssertRepositoryFallbackAsync(finishedGameId);
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await authenticatedClient.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameBoardSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Equal(finishedGameId.ToString(), payload.GameId);
        Assert.Equal(GameStatusValue.Finished, payload.Status);
        Assert.True(payload.Version >= 1);
        Assert.Single(payload.Cells);
    }

    [Fact]
    public async Task Repository_WhenActiveGameHasNoBoard_FallsBackToLatestFinishedGameWithBoard()
    {
        var finishedGameId = await SeedGamesAsync();
        await AssertRepositoryFallbackAsync(finishedGameId);
    }

    [Fact]
    public async Task Repository_WhenNoBoardsExist_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IGameBoardRepository>();

        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var snapshot = await repository.GetCurrentBoardAsync();
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task OpenCell_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        var cellId = await SeedSingleCellAsync();
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await authenticatedClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OpenCell_WhenAdmin_OpensCellAndReturnsNoContent()
    {
        var cellId = await SeedSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cell = await dbContext.BoardCells.FindAsync(cellId);
        Assert.NotNull(cell);
        Assert.Equal(BoardCellState.Open, cell!.State);
    }

    [Fact]
    public async Task OpenCell_WhenAdminAndCellMissing_ReturnsNotFound()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync($"/api/game/cells/{Guid.NewGuid()}/open", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameCellNotFound, payload.Error);
    }

    [Fact]
    public async Task RealtimeSmoke_OpenCell_WhenAdmin_PublishesCellOpenedEvent()
    {
        var cellId = await SeedSingleCellAsync();
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], publisher);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var payload = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(cellId.ToString(), payload.Cell.Id);
        Assert.Equal("open", payload.Cell.State.ToString().ToLowerInvariant());
        Assert.True(payload.Version >= 2);
    }

    [Fact]
    public async Task RealtimeSmoke_OpenCell_WhenCalledTwice_PublishesSingleEvent()
    {
        var cellId = await SeedSingleCellAsync();
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], publisher);

        var firstResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        var secondResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);

        var payload = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(cellId.ToString(), payload.Cell.Id);
        Assert.Equal("open", payload.Cell.State.ToString().ToLowerInvariant());
    }

    private async Task AssertRepositoryFallbackAsync(Guid finishedGameId)
    {
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameBoardRepository>();

        var snapshot = await repository.GetCurrentBoardAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(finishedGameId.ToString(), snapshot.GameId);
    }

    private async Task<Guid> SeedGamesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var finishedGameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();

        dbContext.Games.AddRange(
            new Game
            {
                Id = Guid.NewGuid(),
                Title = "Active without board",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now,
                StartedAtUtc = now
            },
            new Game
            {
                Id = finishedGameId,
                Title = "Finished with board",
                Status = GameStatusValue.Finished,
                CreatedAtUtc = now.AddHours(-2),
                FinishedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = finishedGameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Cell",
                Cost = 100,
                State = BoardCellState.Closed
            }
        );

        await dbContext.SaveChangesAsync();
        return finishedGameId;
    }

    private async Task<Guid> SeedSingleCellAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now,
                StartedAtUtc = now
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Cell",
                Cost = 100,
                State = BoardCellState.Closed
            }
        );

        await dbContext.SaveChangesAsync();
        return cellId;
    }

    private HttpClient CreateAuthenticatedClient(
        string[] roles,
        RecordingGameBoardEventsPublisher? publisher = null
    )
    {
        var authenticatedFactory = CreateAuthenticatedFactory(roles, publisher);
        return authenticatedFactory.CreateClient();
    }

    private WebApplicationFactory<Program> CreateAuthenticatedFactory(
        string[] roles,
        RecordingGameBoardEventsPublisher? publisher = null
    )
    {
        var authenticatedFactory = _factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureServices(
                    services =>
                    {
                        services
                            .RemoveAll<IClaimsTransformation>();
                        services.AddSingleton<IClaimsTransformation, PassthroughClaimsTransformation>();
                        if (publisher is not null)
                        {
                            services.RemoveAll<IGameBoardEventsPublisher>();
                            services.AddSingleton<IGameBoardEventsPublisher>(publisher);
                        }

                        services
                            .AddAuthentication(options =>
                            {
                                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                            })
                            .AddScheme<
                                AuthenticationSchemeOptions,
                                TestAuthenticationHandler
                            >(
                                TestAuthenticationHandler.SchemeName,
                                options => options.ClaimsIssuer = string.Join(',', roles)
                            );
                    }
                )
        );

        return authenticatedFactory;
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "TestAuth";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder
        )
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var roles = (Options.ClaimsIssuer ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(
                claims,
                SchemeName
            );
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class PassthroughClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult(principal);
        }
    }

    private sealed class RecordingGameBoardEventsPublisher : IGameBoardEventsPublisher
    {
        public List<GameCellOpenedEvent> PublishedEvents { get; } = [];

        public Task PublishCellOpenedAsync(
            GameCellOpenedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }
}
