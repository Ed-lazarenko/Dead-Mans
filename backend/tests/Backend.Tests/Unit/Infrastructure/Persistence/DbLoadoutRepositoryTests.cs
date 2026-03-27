using backend.Data;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using backend.Infrastructure.Configuration;
using backend.Infrastructure.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbLoadoutRepositoryTests
{
    private static readonly StorageOptions Storage = new()
    {
        PublicBaseUrl = "https://storage.test.example"
    };

    [Fact]
    public async Task GetBoardAsync_UsesLatestGameBoardByCreatedAtUtc()
    {
        await using var db = CreateContext();
        var old = DateTime.UtcNow.AddDays(-2);
        var latest = DateTime.UtcNow.AddDays(-1);
        var oldGameId = Guid.NewGuid();
        var newGameId = Guid.NewGuid();
        var oldBoardId = Guid.NewGuid();
        var newBoardId = Guid.NewGuid();

        db.Games.AddRange(
            new Game
            {
                Id = oldGameId,
                Title = "Old",
                Status = GameStatusValue.Draft,
                CreatedAtUtc = old
            },
            new Game
            {
                Id = newGameId,
                Title = "New",
                Status = GameStatusValue.Draft,
                CreatedAtUtc = latest
            }
        );
        db.GameBoards.AddRange(
            new GameBoard
            {
                Id = oldBoardId,
                GameId = oldGameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["x"],
                ColLabels = ["y"],
                CreatedAtUtc = old
            },
            new GameBoard
            {
                Id = newBoardId,
                GameId = newGameId,
                Rows = 2,
                Cols = 3,
                RowLabels = ["a", "b"],
                ColLabels = ["1", "2", "3"],
                CreatedAtUtc = latest
            }
        );
        db.BoardCells.Add(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = newBoardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Q",
                Cost = 10,
                State = BoardCellState.Closed,
                CellType = BoardCellPersistence.CellTypeLoadout
            }
        );
        await db.SaveChangesAsync();

        var repo = CreateRepository(db);
        var board = await repo.GetBoardAsync();

        Assert.Equal(2, board.Rows);
        Assert.Equal(3, board.Cols);
        Assert.Equal(["a", "b"], board.RowLabels);
    }

    [Fact]
    public async Task ToggleCellStateAsync_FlipsOpenAndClosed()
    {
        await using var db = CreateContext();
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "G",
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
                Cols = 1,
                RowLabels = ["r"],
                ColLabels = ["c"],
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
                Title = "Cell",
                Cost = 1,
                State = BoardCellState.Closed,
                CellType = BoardCellPersistence.CellTypeLoadout
            }
        );
        await db.SaveChangesAsync();

        var repo = CreateRepository(db);

        var afterOpen = await repo.ToggleCellStateAsync(cellId.ToString());
        Assert.Single(afterOpen.Cells);
        Assert.Equal(LoadoutCellState.Open, FindCell(afterOpen, cellId).State);

        var afterClosed = await repo.ToggleCellStateAsync(cellId.ToString());
        Assert.Equal(LoadoutCellState.Closed, FindCell(afterClosed, cellId).State);
    }

    [Fact]
    public async Task ToggleCellStateAsync_InvalidGuid_ThrowsWithStableMessage()
    {
        await using var db = CreateContext();
        var repo = CreateRepository(db);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ToggleCellStateAsync("nope", CancellationToken.None)
        );

        Assert.Equal(AppMessages.Exceptions.LoadoutCellIdNotValidGuid("nope"), ex.Message);
    }

    [Fact]
    public async Task ToggleCellStateAsync_UnknownCell_Throws()
    {
        await using var db = CreateContext();
        var repo = CreateRepository(db);
        var missingId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ToggleCellStateAsync(missingId.ToString(), CancellationToken.None)
        );

        Assert.Equal(AppMessages.Exceptions.LoadoutCellNotFound(missingId.ToString()), ex.Message);
    }

    [Fact]
    public async Task GetBoardAsync_WhenNoBoardExists_Throws()
    {
        await using var db = CreateContext();
        var repo = CreateRepository(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.GetBoardAsync());
    }

    private static DbLoadoutRepository CreateRepository(ApplicationDbContext db)
    {
        return new DbLoadoutRepository(db, Options.Create(Storage), NullLogger<DbLoadoutRepository>.Instance);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"loadout-repo-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static backend.Domain.Models.LoadoutCell FindCell(backend.Domain.Models.LoadoutBoard board, Guid cellId)
    {
        return board.Cells.Single(c => c.Id == cellId.ToString());
    }
}
