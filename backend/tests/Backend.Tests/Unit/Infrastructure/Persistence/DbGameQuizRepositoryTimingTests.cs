using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Unit.Infrastructure.Persistence;

public sealed class DbGameQuizRepositoryTimingTests
{
    [Fact]
    public async Task AskNextQuizQuestionAsync_WithTwitchDeliveryPersistsSourceMetadata()
    {
        var now = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var gameId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Twitch delivery test",
                Status = GameStatusValue.Active,
                QuizAnswerDurationSeconds = 45,
                CreatedAtUtc = now.UtcDateTime,
                StartedAtUtc = now.UtcDateTime
            }
        );
        dbContext.GameEnabledQuestions.Add(
            new GameEnabledQuestion
            {
                GameId = gameId,
                QuestionId = questionId,
                EnabledAtUtc = now.UtcDateTime,
                QuestionRevisionSnapshot = 1,
                QuestionCodeSnapshot = "twitch-question",
                CategoryNameSnapshot = "twitch",
                QuestionTextSnapshot = "Answer?",
                AcceptedAnswersSnapshot = ["answer"],
                NormalizedAnswersSnapshot = ["answer"],
                RewardSnapshot = 5,
                PrioritySnapshot = 1,
                SnapshotAtUtc = now.UtcDateTime
            }
        );
        await dbContext.SaveChangesAsync();
        var repository = new DbGameQuizRepository(dbContext, new FixedTimeProvider(now));

        var result = await repository.AskNextQuizQuestionAsync(
            gameId,
            new TwitchGameQuizQuestionDelivery("channel-1", "message-1")
        );

        Assert.NotNull(result);
        var round = await dbContext.GameQuizRounds.SingleAsync();
        Assert.Equal(GameQuizDeliveryKindValue.Twitch, round.DeliveryKind);
        Assert.Equal("channel-1", round.SourceChannelId);
        Assert.Equal("message-1", round.SourceMessageId);
        Assert.Null(round.AskedByUserId);
        Assert.Equal(now.AddSeconds(45).UtcDateTime, round.ClosesAtUtc);
    }

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
            new SubmitGameQuizAnswerInput(
                "answer",
                new ManualGameQuizAnswerSource(seeded.UserId, seeded.UserId, "Viewer")
            )
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
            new SubmitGameQuizAnswerInput(
                "answer",
                new ManualGameQuizAnswerSource(seeded.UserId, seeded.UserId, "Viewer")
            )
        );

        Assert.Equal(SubmitQuizAnswerRepositoryOutcome.Correct, result.Outcome);
        Assert.Equal(GameQuizRoundStatusValue.AnsweredCorrect, result.Round?.Status);
        var answer = await dbContext.GameQuizCorrectAnswers.SingleAsync();
        Assert.Equal(deadline.AddTicks(-1).UtcDateTime, answer.AnsweredAtUtc);
        var reward = await dbContext.GameQuizPointLedgerEntries.SingleAsync();
        Assert.Equal(5, reward.PointsDelta);
        Assert.Equal(answer.Id, reward.CorrectAnswerId);
    }

    [Fact]
    public async Task AnswerQuizRoundAsync_WithCorrectTwitchAnswerCreatesPreLoginPrincipal()
    {
        var deadline = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var seeded = await SeedOpenRoundAsync(dbContext, deadline.UtcDateTime);
        dbContext.Users.Remove(await dbContext.Users.SingleAsync());
        await dbContext.SaveChangesAsync();
        var answeredAt = deadline.AddSeconds(-1);
        var repository = new DbGameQuizRepository(
            dbContext,
            new FixedTimeProvider(answeredAt)
        );

        var result = await repository.AnswerQuizRoundAsync(
            seeded.RoundId,
            new SubmitGameQuizAnswerInput(
                "answer",
                new TwitchGameQuizAnswerSource(
                    "987654",
                    "new_viewer",
                    "New Viewer",
                    "channel-1",
                    "message-2"
                )
            )
        );

        Assert.Equal(SubmitQuizAnswerRepositoryOutcome.Correct, result.Outcome);
        var user = await dbContext.Users.SingleAsync();
        Assert.Equal("987654", user.TwitchUserId);
        Assert.Equal("new_viewer", user.Login);
        Assert.Equal("New Viewer", user.DisplayName);
        Assert.Null(user.LastLoginAtUtc);
        Assert.Empty(user.UserRoles);
        var answer = await dbContext.GameQuizCorrectAnswers.SingleAsync();
        Assert.Equal(user.Id, answer.AwardedToUserId);
        Assert.Null(answer.CapturedByUserId);
        Assert.Equal(GameQuizAnswerSourceValue.Twitch, answer.SourceProvider);
        Assert.Equal("channel-1", answer.SourceChannelId);
        Assert.Equal("message-2", answer.SourceMessageId);
        Assert.Equal(answeredAt.UtcDateTime, answer.AnsweredAtUtc);
    }

    [Fact]
    public async Task AnswerQuizRoundAsync_WithIncorrectTwitchAnswerDoesNotCreatePrincipalOrHistory()
    {
        var deadline = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = CreateDbContext();
        var seeded = await SeedOpenRoundAsync(dbContext, deadline.UtcDateTime);
        dbContext.Users.Remove(await dbContext.Users.SingleAsync());
        await dbContext.SaveChangesAsync();
        var repository = new DbGameQuizRepository(
            dbContext,
            new FixedTimeProvider(deadline.AddSeconds(-1))
        );

        var result = await repository.AnswerQuizRoundAsync(
            seeded.RoundId,
            new SubmitGameQuizAnswerInput(
                "wrong",
                new TwitchGameQuizAnswerSource(
                    "987654",
                    "new_viewer",
                    "New Viewer",
                    "channel-1",
                    "message-3"
                )
            )
        );

        Assert.Equal(SubmitQuizAnswerRepositoryOutcome.Incorrect, result.Outcome);
        Assert.Empty(dbContext.Users);
        Assert.Empty(dbContext.GameQuizCorrectAnswers);
        Assert.Empty(dbContext.GameQuizPointLedgerEntries);
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
