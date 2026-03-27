using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Backend.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration.Loadout;

public sealed class LoadoutContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoadoutContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLoadout_WhenBoardExists_ReturnsCellsAndLabels()
    {
        var cellId = await SeedMinimalBoardAsync();

        var response = await _client.GetAsync("/api/loadout");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var board = await response.Content.ReadFromJsonAsync<LoadoutBoardDto>();
        Assert.NotNull(board);
        Assert.Equal(1, board.Rows);
        Assert.Equal(2, board.Cols);
        Assert.Equal(["R0"], board.RowLabels);
        Assert.Equal(["C0", "C1"], board.ColLabels);
        Assert.Single(board.Cells);
        Assert.Equal(cellId.ToString(), board.Cells[0].Id);
        Assert.Equal("closed", board.Cells[0].State);
    }

    [Fact]
    public async Task ToggleLoadoutCell_WhenInvalidId_ReturnsBadRequestWithErrorPayload()
    {
        await SeedMinimalBoardAsync();

        var response = await _client.PostAsync("/api/loadout/not-a-guid/toggle", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Contains("not a valid GUID", error.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ToggleLoadoutCell_WhenValidId_FlipsOpenState()
    {
        var cellId = await SeedMinimalBoardAsync();

        var toggle1 = await _client.PostAsync($"/api/loadout/{cellId}/toggle", content: null);
        Assert.Equal(HttpStatusCode.OK, toggle1.StatusCode);
        var afterOpen = await toggle1.Content.ReadFromJsonAsync<LoadoutBoardDto>();
        Assert.NotNull(afterOpen);
        Assert.Equal("open", afterOpen.Cells[0].State);

        var toggle2 = await _client.PostAsync($"/api/loadout/{cellId}/toggle", content: null);
        Assert.Equal(HttpStatusCode.OK, toggle2.StatusCode);
        var afterClosed = await toggle2.Content.ReadFromJsonAsync<LoadoutBoardDto>();
        Assert.NotNull(afterClosed);
        Assert.Equal("closed", afterClosed.Cells[0].State);
    }

    private async Task<Guid> SeedMinimalBoardAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.BoardCells.RemoveRange(db.BoardCells);
        db.GameBoards.RemoveRange(db.GameBoards);
        db.Games.RemoveRange(db.Games);
        await db.SaveChangesAsync();

        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.Games.Add(
            new backend.Data.Entities.Game
            {
                Id = gameId,
                Title = "Seed game",
                Status = GameStatusValue.Draft,
                CreatedAtUtc = now
            }
        );

        db.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 2,
                RowLabels = ["R0"],
                ColLabels = ["C0", "C1"],
                CreatedAtUtc = now
            }
        );

        db.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Q1",
                Cost = 5,
                State = BoardCellState.Closed,
                CellType = BoardCellPersistence.CellTypeLoadout
            }
        );

        await db.SaveChangesAsync();
        return cellId;
    }
}
