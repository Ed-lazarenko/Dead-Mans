using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
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
    public async Task GetLatestBoardByStatusAsync_ForActive_ReturnsLatestActiveGameThatHasBoard()
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
                CellType = BoardCellPersistence.DefaultCellType
            }
        );

        await db.SaveChangesAsync();

        IGameBoardRepository repo = new DbGameBoardRepository(
            db,
            Options.Create(Storage),
            NullLogger<DbGameBoardRepository>.Instance,
            TimeProvider.System
        );

        var snapshot = await repo.GetLatestBoardByStatusAsync(GameStatusValue.Active);

        Assert.NotNull(snapshot);
        Assert.Equal(newerActiveId.ToString(), snapshot.GameId);
        Assert.Equal(2, snapshot.Rows);
    }

    [Fact]
    public async Task GetLatestBoardByStatusAsync_ForFinished_ReturnsLatestFinishedGameThatHasBoard()
    {
        await using var db = CreateContext();
        var t0 = DateTime.UtcNow.AddHours(-2);
        var t1 = DateTime.UtcNow.AddHours(-1);

        var finishedOlderId = Guid.NewGuid();
        var finishedNewerId = Guid.NewGuid();
        var finishedOlderBoardId = Guid.NewGuid();
        var finishedNewerBoardId = Guid.NewGuid();

        db.Games.AddRange(
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
                CellType = BoardCellPersistence.DefaultCellType
            }
        );

        await db.SaveChangesAsync();

        IGameBoardRepository repo = new DbGameBoardRepository(
            db,
            Options.Create(Storage),
            NullLogger<DbGameBoardRepository>.Instance,
            TimeProvider.System
        );

        var snapshot = await repo.GetLatestBoardByStatusAsync(GameStatusValue.Finished);

        Assert.NotNull(snapshot);
        Assert.Equal(finishedNewerId.ToString(), snapshot.GameId);
        Assert.Equal(3, snapshot.Rows);
    }

    [Fact]
    public async Task TryOpenCellAsync_WhenCalledTwice_ChangesStateOnlyOnceAndIncrementsVersionOnce()
    {
        await using var db = CreateContext();
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();

        db.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = DateTime.UtcNow,
                StartedAtUtc = DateTime.UtcNow
            }
        );
        db.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        db.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Secret title",
                Description = "Secret description",
                State = BoardCellState.Closed,
                Cost = 100,
                CellType = BoardCellPersistence.DefaultCellType
            }
        );
        await db.SaveChangesAsync();

        IGameBoardRepository repo = new DbGameBoardRepository(
            db,
            Options.Create(Storage),
            NullLogger<DbGameBoardRepository>.Instance,
            TimeProvider.System
        );

        var snapshot = await repo.GetLatestBoardByStatusAsync(GameStatusValue.Active);
        Assert.NotNull(snapshot);
        var closedCell = Assert.Single(snapshot.Cells);
        Assert.Null(closedCell.Title);
        Assert.Null(closedCell.Description);

        var first = await repo.TryOpenCellAsync(cellId);
        var second = await repo.TryOpenCellAsync(cellId);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first!.StateChanged);
        Assert.False(second!.StateChanged);
        Assert.Equal(2, first.Version);
        Assert.Equal(2, second.Version);
    }

    [Fact]
    public async Task SetGameTeamPlayedStateAsync_UsesInjectedClock()
    {
        await using var db = CreateContext();
        var timestamp = new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var previousTimestamp = timestamp.AddDays(-1).UtcDateTime;
        var gameId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        db.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Clock game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = previousTimestamp,
                StartedAtUtc = previousTimestamp
            }
        );
        db.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = previousTimestamp
            }
        );
        db.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                Name = "Clock team",
                Status = TeamStatusValue.Confirmed,
                CreatedAtUtc = previousTimestamp,
                UpdatedAtUtc = previousTimestamp,
                ConfirmedAtUtc = previousTimestamp
            }
        );
        await db.SaveChangesAsync();
        IGameBoardRepository repository = new DbGameBoardRepository(
            db,
            Options.Create(Storage),
            NullLogger<DbGameBoardRepository>.Instance,
            new FixedTimeProvider(timestamp)
        );

        var outcome = await repository.SetGameTeamPlayedStateAsync(teamId, isPlayed: true);

        Assert.Equal(SetGameTeamPlayedStateOutcome.Updated, outcome);
        var team = await db.GameTeams.SingleAsync();
        Assert.True(team.IsPlayed);
        Assert.Equal(timestamp.UtcDateTime, team.PlayedAtUtc);
        Assert.Equal(timestamp.UtcDateTime, team.UpdatedAtUtc);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"game-board-repo-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
