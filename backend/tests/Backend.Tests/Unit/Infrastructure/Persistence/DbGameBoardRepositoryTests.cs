using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Configuration;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameBoardRepositoryTests
{
    private static readonly StorageOptions Storage = new() { PublicBaseUrl = "https://cdn.example" };

    [Fact]
    public async Task GetCurrentBoardAsync_PrefersLatestActiveGameThatHasBoard()
    {
        await using var db = CreateContext();
        var t0 = DateTime.UtcNow.AddHours(-3);
        var t1 = DateTime.UtcNow.AddHours(-2);
        var t2 = DateTime.UtcNow.AddHours(-1);

        var olderActiveId = Guid.NewGuid();
        var newerActiveId = Guid.NewGuid();
        var finishedId = Guid.NewGuid();
        var olderActiveBoardId = Guid.NewGuid();
        var newerActiveBoardId = Guid.NewGuid();
        var finishedBoardId = Guid.NewGuid();

        db.Games.AddRange(
            new Game
            {
                Id = olderActiveId,
                Title = "Older active",
                Status = GameStatusValue.Active,
                CreatedAtUtc = t0,
                StartedAtUtc = t0
            },
            new Game
            {
                Id = newerActiveId,
                Title = "Newer active",
                Status = GameStatusValue.Active,
                CreatedAtUtc = t1,
                StartedAtUtc = t1
            },
            new Game
            {
                Id = finishedId,
                Title = "Finished",
                Status = GameStatusValue.Finished,
                CreatedAtUtc = t0,
                FinishedAtUtc = t2
            }
        );

        db.GameBoards.AddRange(
            new GameBoard
            {
                Id = olderActiveBoardId,
                GameId = olderActiveId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["a"],
                ColLabels = ["b"],
                CreatedAtUtc = t0
            },
            new GameBoard
            {
                Id = newerActiveBoardId,
                GameId = newerActiveId,
                Rows = 2,
                Cols = 2,
                RowLabels = ["n1", "n2"],
                ColLabels = ["x", "y"],
                CreatedAtUtc = t1
            },
            new GameBoard
            {
                Id = finishedBoardId,
                GameId = finishedId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["f"],
                ColLabels = ["f"],
                CreatedAtUtc = t2
            }
        );

        db.BoardCells.Add(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = newerActiveBoardId,
                RowIndex = 0,
                ColIndex = 0,
                State = BoardCellState.Closed,
                Cost = 1,
                CellType = BoardCellPersistence.CellTypeLoadout
            }
        );

        await db.SaveChangesAsync();

        IGameBoardRepository repo = new DbGameBoardRepository(
            db,
            Options.Create(Storage),
            NullLogger<DbGameBoardRepository>.Instance
        );

        var snapshot = await repo.GetCurrentBoardAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(newerActiveId.ToString(), snapshot.GameId);
        Assert.Equal(2, snapshot.Rows);
    }

    [Fact]
    public async Task GetCurrentBoardAsync_WhenNoActiveWithBoard_FallsBackToLatestFinishedWithBoard()
    {
        await using var db = CreateContext();
        var t0 = DateTime.UtcNow.AddHours(-2);
        var t1 = DateTime.UtcNow.AddHours(-1);

        var activeNoBoardId = Guid.NewGuid();
        var finishedOlderId = Guid.NewGuid();
        var finishedNewerId = Guid.NewGuid();
        var finishedOlderBoardId = Guid.NewGuid();
        var finishedNewerBoardId = Guid.NewGuid();

        db.Games.AddRange(
            new Game
            {
                Id = activeNoBoardId,
                Title = "Active",
                Status = GameStatusValue.Active,
                CreatedAtUtc = t1,
                StartedAtUtc = t1
            },
            new Game
            {
                Id = finishedOlderId,
                Title = "Finished old",
                Status = GameStatusValue.Finished,
                CreatedAtUtc = t0,
                FinishedAtUtc = t0.AddMinutes(30)
            },
            new Game
            {
                Id = finishedNewerId,
                Title = "Finished new",
                Status = GameStatusValue.Finished,
                CreatedAtUtc = t0,
                FinishedAtUtc = t1
            }
        );

        db.GameBoards.AddRange(
            new GameBoard
            {
                Id = finishedOlderBoardId,
                GameId = finishedOlderId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["o"],
                ColLabels = ["o"],
                CreatedAtUtc = t0
            },
            new GameBoard
            {
                Id = finishedNewerBoardId,
                GameId = finishedNewerId,
                Rows = 3,
                Cols = 1,
                RowLabels = ["a", "b", "c"],
                ColLabels = ["z"],
                CreatedAtUtc = t1
            }
        );

        db.BoardCells.Add(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = finishedNewerBoardId,
                RowIndex = 0,
                ColIndex = 0,
                State = BoardCellState.Closed,
                Cost = 5,
                CellType = BoardCellPersistence.CellTypeLoadout
            }
        );

        await db.SaveChangesAsync();

        IGameBoardRepository repo = new DbGameBoardRepository(
            db,
            Options.Create(Storage),
            NullLogger<DbGameBoardRepository>.Instance
        );

        var snapshot = await repo.GetCurrentBoardAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(finishedNewerId.ToString(), snapshot.GameId);
        Assert.Equal(3, snapshot.Rows);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"game-board-repo-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }
}
