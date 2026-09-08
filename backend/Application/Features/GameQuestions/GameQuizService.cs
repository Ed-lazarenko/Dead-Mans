using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GameQuizService> _logger;

    public GameQuizService(
        IGameQuizRepository repository,
        IGameBoardEventsPublisher eventsPublisher,
        TimeProvider timeProvider,
        ILogger<GameQuizService> logger
    )
    {
        _repository = repository;
        _eventsPublisher = eventsPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AskNextGameQuizQuestionResult> AskNextQuizQuestionAsync(
        GameQuizQuestionDelivery delivery,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsValidDelivery(delivery))
        {
            return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.InvalidDelivery);
        }

        var activeGameId = await _repository.GetActiveGameIdAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.NoActiveGame);
        }

        var askedQuestion = await _repository.AskNextQuizQuestionAsync(
            activeGameId.Value,
            delivery,
            cancellationToken
        );
        if (askedQuestion is null)
        {
            return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.NoAvailableQuestions);
        }

        await PublishQuizStateChangedBestEffortAsync(
            askedQuestion.GameId,
            GameQuizStateChangeKinds.QuestionAsked,
            askedQuestion.AskedAtUtc
        );

        return new AskNextGameQuizQuestionResult(AskNextGameQuizQuestionOutcome.Asked, askedQuestion);
    }

    public async Task<AnswerGameQuizRoundResult> AnswerQuizRoundAsync(
        Guid roundId,
        SubmitGameQuizAnswerInput input,
        CancellationToken cancellationToken = default
    )
    {
        if (
            string.IsNullOrWhiteSpace(input.SubmittedAnswer)
            || input.SubmittedAnswer.Trim().Length > 500
        )
        {
            return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.InvalidAnswer);
        }
        if (!IsValidAnswerSource(input.Source))
        {
            return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.InvalidSource);
        }

        var submission = await _repository.AnswerQuizRoundAsync(
            roundId,
            input,
            cancellationToken
        );
        if (submission.Outcome == SubmitQuizAnswerRepositoryOutcome.RoundNotFound)
        {
            return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.QuizRoundNotFound);
        }
        if (submission.Outcome == SubmitQuizAnswerRepositoryOutcome.RoundNotPending)
        {
            return new AnswerGameQuizRoundResult(
                AnswerGameQuizRoundOutcome.QuizRoundNotPending,
                submission.Round
            );
        }
        if (submission.Outcome == SubmitQuizAnswerRepositoryOutcome.Incorrect)
        {
            return new AnswerGameQuizRoundResult(
                AnswerGameQuizRoundOutcome.Incorrect,
                submission.Round
            );
        }
        if (submission.Outcome == SubmitQuizAnswerRepositoryOutcome.PlayerNotFound)
        {
            return new AnswerGameQuizRoundResult(AnswerGameQuizRoundOutcome.PlayerNotFound);
        }

        await PublishQuizStateChangedBestEffortAsync(
            submission.Round!.GameId,
            GameQuizStateChangeKinds.QuestionAnswered,
            submission.Round.AnsweredAtUtc ?? _timeProvider.GetUtcNow().UtcDateTime
        );

        return new AnswerGameQuizRoundResult(
            AnswerGameQuizRoundOutcome.Answered,
            submission.Round
        );
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
        if (!GameQuizManualAdjustmentOperationValue.All.Contains(input.OperationType))
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.InvalidOperation);
        }
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length is < 3 or > 500)
        {
            return new ManualQuizAwardResult(ManualQuizAwardOutcome.InvalidReason);
        }

        var result = await _repository.AwardManualQuizPointsAsync(
            input,
            awardedByUserId,
            cancellationToken
        );

        if (result.Outcome == ManualQuizAwardOutcome.Awarded
            && result.Award is not null
            && result.StateChanged)
        {
            await PublishQuizStateChangedBestEffortAsync(
                result.Award.GameId,
                GameQuizStateChangeKinds.ManualAdjustmentApplied,
                result.Award.AwardedAtUtc
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
        DateTime occurredAtUtc
    )
    {
        return RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishQuizStateChangedAsync(
                new GameQuizStateChangedEvent(gameId, changeKind, occurredAtUtc),
                publishToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameQuizStateChangedPublishFailed,
            gameId,
            changeKind
        );
    }

    private static bool IsValidDelivery(GameQuizQuestionDelivery delivery)
    {
        return delivery switch
        {
            ManualGameQuizQuestionDelivery manual => manual.AskedByUserId != Guid.Empty,
            TwitchGameQuizQuestionDelivery twitch =>
                HasRequiredValue(twitch.SourceChannelId, 128)
                && HasValidOptionalValue(twitch.SourceMessageId, 128),
            _ => false
        };
    }

    private static bool IsValidAnswerSource(GameQuizAnswerSource source)
    {
        return source switch
        {
            ManualGameQuizAnswerSource manual =>
                manual.CapturedByUserId != Guid.Empty
                && manual.AwardedToUserId != Guid.Empty
                && HasValidOptionalValue(manual.ReportedDisplayName, 128),
            TwitchGameQuizAnswerSource twitch =>
                TwitchIdentityValidator.IsValid(
                    twitch.TwitchUserId,
                    twitch.Login,
                    twitch.DisplayName
                )
                && HasRequiredValue(twitch.SourceChannelId, 128)
                && HasRequiredValue(twitch.SourceMessageId, 128),
            _ => false
        };
    }

    private static bool HasRequiredValue(string? value, int maximumLength)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;
    }

    private static bool HasValidOptionalValue(string? value, int maximumLength)
    {
        return value is null || (!string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength);
    }
}
