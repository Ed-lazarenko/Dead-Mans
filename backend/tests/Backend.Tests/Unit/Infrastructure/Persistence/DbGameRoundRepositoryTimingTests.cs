using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameRoundRepositoryTimingTests
{
    [Fact]
    public async Task TechnicalCancelAsync_UsesInjectedClockForRoundAndAudit()
    {
        var timestamp = new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
        var previousTimestamp = timestamp.AddDays(-1).UtcDateTime;
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var roundId = Guid.NewGuid();
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = "round-clock-user",
                Login = "round-clock-user",
                DisplayName = "Round Clock User",
                IsActive = true,
                CreatedAtUtc = previousTimestamp,
                UpdatedAtUtc = previousTimestamp
            }
        );
        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Round clock game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = previousTimestamp,
                StartedAtUtc = previousTimestamp,
                ActiveTeamId = teamId
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
                Version = 1,
                CreatedAtUtc = previousTimestamp
            }
        );
        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                State = BoardCellState.Open,
                Title = "Clock cell",
                Cost = 100
            }
        );
        dbContext.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = previousTimestamp
            }
        );
        dbContext.GameTeams.Add(
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
        dbContext.GameRounds.Add(
            new GameRound
            {
                Id = roundId,
                GameId = gameId,
                BoardId = boardId,
                BoardCellId = cellId,
                TeamId = teamId,
                Status = GameRoundStatusValue.AwaitingModifiers,
                Version = 1,
                BaseScore = 100,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Clock cell",
                CellCostSnapshot = 100,
                CreatedAtUtc = previousTimestamp,
                UpdatedAtUtc = previousTimestamp
            }
        );
        await dbContext.SaveChangesAsync();
        var repository = new DbGameRoundRepository(
            dbContext,
            new FixedTimeProvider(timestamp)
        );

        var result = await repository.TechnicalCancelAsync(
            roundId,
            new TechnicalCancelGameRoundInput(
                ExpectedRoundVersion: 1,
                GameRoundTechnicalCancellationReasonValue.ApplicationError,
                InternalDetail: "Deterministic clock test",
                PublicSummary: null
            ),
            userId
        );

        Assert.Equal(TransitionGameRoundOutcome.Transitioned, result.Outcome);
        var round = await dbContext.GameRounds.SingleAsync();
        Assert.Equal(GameRoundStatusValue.Cancelled, round.Status);
        Assert.Equal(timestamp.UtcDateTime, round.FinishedAtUtc);
        Assert.Equal(timestamp.UtcDateTime, round.UpdatedAtUtc);
        var audit = await dbContext.GameRoundTransitionAudits.SingleAsync();
        Assert.Equal(timestamp.UtcDateTime, audit.OccurredAtUtc);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
