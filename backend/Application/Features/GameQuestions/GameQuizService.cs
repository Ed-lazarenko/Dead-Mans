using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Domain.Persistence;
using backend.Messaging;

namespace backend.Application.Features.GameQuestions;

public sealed class GameQuizService : IGameQuizService
{
    private readonly IGameQuizRepository _repository;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameQuizService> _logger;

    public GameQuizService(
        IGameQuizRepository repository,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameQuizService> logger
    )
    {
        _repository = repository;
        _eventsPublisher = eventsPublisher;
        _logger = logger;
    }

    public async Task<AskNextGameQuizQuestionResult> AskNextQuizQuestionAsync(
        Guid? askedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await _repository.GetActiveGameIdAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.NoActiveGame);
        }

        var askedQuestion = await _repository.AskNextQuizQuestionAsync(
            activeGameId.Value,
            askedByUserId,
            cancellationToken
        );
        if (askedQuestion is null)
        {
            return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.NoAvailableQuestions);
        }

        await PublishQuizStateChangedBestEffortAsync(
            askedQuestion.GameId,
            GameQuizStateChangeKinds.QuestionAsked,
            askedQuestion.AskedAtUtc,
            cancellationToken
        );

        return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.Asked, askedQuestion);
    }

    public async Task<AnswerGameQuizRoundResult> AnswerQuizRoundAsync(
        Guid roundId,
        string submittedAnswer,
        Guid? answeredByUserId,
        Guid? answeredForUserId,
        string? answeredByDisplayName,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(submittedAnswer))
        {
            return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.InvalidAnswer);
        }

        var round = await _repository.GetQuizRoundAsync(roundId, cancellationToken);
        if (round is null)
        {
            return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.QuizRoundNotFound);
        }

        if (round.Status != GameQuizRoundStatusValue.Asked)
        {
            return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.QuizRoundNotPending, round);
        }

        var updatedRound = await _repository.AnswerQuizRoundAsync(
            roundId,
            answeredByUserId,
            answeredForUserId,
            answeredByDisplayName,
            submittedAnswer,
            cancellationToken
        );
        if (updatedRound is null)
        {
            return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.QuizRoundNotPending, round);
        }

        await PublishQuizStateChangedBestEffortAsync(
            updatedRound.GameId,
            GameQuizStateChangeKinds.QuestionAnswered,
            updatedRound.AnsweredAtUtc ?? DateTime.UtcNow,
            cancellationToken
        );

        return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.Answered, updatedRound);
    }

    public async Task<ManualQuizAwardResult> AwardManualQuizPointsAsync(
        ManualQuizAwardInput input,
        Guid awardedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (input.Points <= 0)
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.InvalidPoints);
        }

        var result = await _repository.AwardManualQuizPointsAsync(
            input,
            awardedByUserId,
            cancellationToken
        );

        if (result.Outcome == ManualQuizAwardOutcome.Awarded && result.Award is not null)
        {
            await PublishQuizStateChangedBestEffortAsync(
                result.Award.GameId,
                GameQuizStateChangeKinds.ManualAwardGranted,
                result.Award.AwardedAtUtc,
                cancellationToken
            );
        }

        return result;
    }

    public Task<IReadOnlyList<ManualQuizAwardPlayer>> GetManualQuizAwardPlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetManualQuizAwardPlayersAsync(cancellationToken);
    }

    private Task PublishQuizStateChangedBestEffortAsync(
        Guid gameId,
        string changeKind,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken
    )
    {
        return RealtimePublishGuard.TryPublishAsync(
            () => _eventsPublisher.PublishQuizStateChangedAsync(
                new GameQuizStateChangedEvent(gameId, changeKind, occurredAtUtc),
                cancellationToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameQuizStateChangedPublishFailed,
            gameId,
            changeKind
        );
    }
}
