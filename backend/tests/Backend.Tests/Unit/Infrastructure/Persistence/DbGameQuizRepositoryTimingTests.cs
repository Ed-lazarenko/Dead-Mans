using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameQuizRepositoryTimingTests
{
    [Fact]
    public async Task AnswerQuizRoundAsync_AtExactDeadlineTimesOutWithoutPersistingAnswer()
    {
        var deadline = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var seeded = await SeedOpenRoundAsync(dbContext, deadline.UtcDateTime);
        var repository = new DbGameQuizRepository(
            dbContext,
            new FixedTimeProvider(deadline)
        );

        var result = await repository.AnswerQuizRoundAsync(
            seeded.RoundId,
            seeded.UserId,
            answeredForUserId: null,
            answeredByDisplayName: "Viewer",
            submittedAnswer: "answer"
        );

        Assert.Equal(SubmitQuizAnswerRepositoryOutcome.RoundNotPending, result.Outcome);
        Assert.Equal(GameQuizRoundStatusValue.Timeout, result.Round?.Status);
        Assert.False(await dbContext.GameQuizCorrectAnswers.AnyAsync());
        Assert.False(await dbContext.GameQuizPointLedgerEntries.AnyAsync());
    }

    [Fact]
    public async Task AnswerQuizRoundAsync_BeforeDeadlinePersistsFirstCorrectAnswerAndReward()
    {
        var deadline = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var seeded = await SeedOpenRoundAsync(dbContext, deadline.UtcDateTime);
        var repository = new DbGameQuizRepository(
            dbContext,
            new FixedTimeProvider(deadline.AddTicks(-1))
        );

        var result = await repository.AnswerQuizRoundAsync(
            seeded.RoundId,
            seeded.UserId,
            answeredForUserId: null,
            answeredByDisplayName: "Viewer",
            submittedAnswer: "answer"
        );

        Assert.Equal(SubmitQuizAnswerRepositoryOutcome.Correct, result.Outcome);
        Assert.Equal(GameQuizRoundStatusValue.AnsweredCorrect, result.Round?.Status);
        var answer = await dbContext.GameQuizCorrectAnswers.SingleAsync();
        Assert.Equal(deadline.AddTicks(-1).UtcDateTime, answer.AnsweredAtUtc);
        var reward = await dbContext.GameQuizPointLedgerEntries.SingleAsync();
        Assert.Equal(5, reward.PointsDelta);
        Assert.Equal(answer.Id, reward.CorrectAnswerId);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"quiz-timing-tests-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<SeededQuizRound> SeedOpenRoundAsync(
        ApplicationDbContext dbContext,
        DateTime closesAtUtc
    )
    {
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roundId = Guid.NewGuid();
        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Timing test",
                Status = GameStatusValue.Active,
                IsDeleted = false,
                CreatedAtUtc = closesAtUtc.AddMinutes(-5),
                StartedAtUtc = closesAtUtc.AddMinutes(-5)
            }
        );
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = "123456",
                Login = "viewer",
                DisplayName = "Viewer",
                IsActive = true,
                CreatedAtUtc = closesAtUtc.AddMinutes(-5),
                UpdatedAtUtc = closesAtUtc.AddMinutes(-5)
            }
        );
        dbContext.GameQuizRounds.Add(
            new GameQuizRound
            {
                Id = roundId,
                GameId = gameId,
                QuestionId = Guid.NewGuid(),
                AskOrder = 1,
                AskedAtUtc = closesAtUtc.AddMinutes(-1),
                ClosesAtUtc = closesAtUtc,
                Status = GameQuizRoundStatusValue.Asked,
                QuestionRevisionSnapshot = 1,
                QuestionCodeSnapshot = "timing-question",
                CategoryNameSnapshot = "timing",
                QuestionTextSnapshot = "Answer?",
                AcceptedAnswersSnapshot = ["answer"],
                NormalizedAnswersSnapshot = ["answer"],
                RewardSnapshot = 5,
                DeliveryKind = GameQuizDeliveryKindValue.Manual
            }
        );
        await dbContext.SaveChangesAsync();
        return new SeededQuizRound(roundId, userId);
    }

    private sealed record SeededQuizRound(Guid RoundId, Guid UserId);

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
