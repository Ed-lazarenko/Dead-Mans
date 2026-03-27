using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration;

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
    public async Task GetGame_WhenActiveGameHasNoBoard_FallsBackToLatestFinishedGameWithBoard()
    {
        var finishedGameId = await SeedGamesAsync();
        await AssertRepositoryFallbackAsync(finishedGameId);

        var response = await _client.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GameBoardSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Equal(finishedGameId.ToString(), payload.GameId);
        Assert.Equal(GameStatusValue.Finished, payload.Status);
        Assert.Single(payload.Cells);
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
}
