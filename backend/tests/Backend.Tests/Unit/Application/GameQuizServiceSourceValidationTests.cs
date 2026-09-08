using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Features.GameQuestions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Unit.Application;

public sealed class GameQuizServiceSourceValidationTests
{
    [Fact]
    public async Task AskNextQuizQuestionAsync_WhenTwitchDeliveryIsInvalidRejectsBeforeRepository()
    {
        var repository = new TrackingQuizRepository();
        var service = CreateService(repository);

        var result = await service.AskNextQuizQuestionAsync(
            new TwitchGameQuizQuestionDelivery(" ", "message")
        );

        Assert.Equal(AskNextGameQuizQuestionOutcome.InvalidDelivery, result.Outcome);
        Assert.False(repository.WasCalled);
    }

    [Theory]
    [InlineData("not-numeric", "viewer", "Viewer", "channel", "message")]
    [InlineData("123456", " ", "Viewer", "channel", "message")]
    [InlineData("123456", "viewer", "Viewer", " ", "message")]
    [InlineData("123456", "viewer", "Viewer", "channel", " ")]
    public async Task AnswerQuizRoundAsync_WhenTwitchSourceIsInvalidRejectsBeforeRepository(
        string twitchUserId,
        string login,
        string displayName,
        string channelId,
        string messageId
    )
    {
        var repository = new TrackingQuizRepository();
        var service = CreateService(repository);

        var result = await service.AnswerQuizRoundAsync(
            Guid.NewGuid(),
            new SubmitGameQuizAnswerInput(
                "answer",
                new TwitchGameQuizAnswerSource(
                    twitchUserId,
                    login,
                    displayName,
                    channelId,
                    messageId
                )
            )
        );

        Assert.Equal(AnswerGameQuizRoundOutcome.InvalidSource, result.Outcome);
        Assert.False(repository.WasCalled);
    }

    private static GameQuizService CreateService(IGameQuizRepository repository)
    {
        return new GameQuizService(
            repository,
            eventsPublisher: null!,
            TimeProvider.System,
            NullLogger<GameQuizService>.Instance
        );
    }

    private sealed class TrackingQuizRepository : IGameQuizRepository
    {
        public bool WasCalled { get; private set; }

        public Task<Guid?> GetActiveGameIdAsync(CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            throw new NotSupportedException();
        }

        public Task<AskedQuizQuestion?> AskNextQuizQuestionAsync(
            Guid gameId,
            GameQuizQuestionDelivery delivery,
            CancellationToken cancellationToken = default
        )
        {
            WasCalled = true;
            throw new NotSupportedException();
        }

        public Task<SubmitQuizAnswerRepositoryResult> AnswerQuizRoundAsync(
            Guid roundId,
            SubmitGameQuizAnswerInput input,
            CancellationToken cancellationToken = default
        )
        {
            WasCalled = true;
            throw new NotSupportedException();
        }

        public Task<ManualQuizAwardResult> AwardManualQuizPointsAsync(
            ManualQuizAwardInput input,
            Guid awardedByUserId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<ManualQuizAwardPlayer>> GetManualQuizAwardPlayersAsync(
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<GameQuizRoundSummary?> GetQuizRoundAsync(
            Guid roundId,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}
