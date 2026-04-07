using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using backend.Api.Contracts;
using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Backend.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
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
        using var authenticatedClient = CreateAuthenticatedClient();

        var response = await authenticatedClient.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameBoardSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Equal(finishedGameId.ToString(), payload.GameId);
        Assert.Equal(GameStatusValue.Finished, payload.Status);
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

    private HttpClient CreateAuthenticatedClient()
    {
        var authenticatedFactory = _factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureServices(
                    services =>
                    {
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
                            >(TestAuthenticationHandler.SchemeName, _ => { });
                    }
                )
        );

        return authenticatedFactory.CreateClient();
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
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                SchemeName
            );
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
