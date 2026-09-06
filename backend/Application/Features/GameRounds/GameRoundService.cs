using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Messaging;

namespace backend.Application.Features.GameRounds;

public sealed class GameRoundService : IGameRoundService
{
    private readonly IGameRoundRepository _repository;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameRoundService> _logger;

    public GameRoundService(
        IGameRoundRepository repository,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameRoundService> logger
    )
    {
        _repository = repository;
        _eventsPublisher = eventsPublisher;
        _logger = logger;
    }

    public Task<IReadOnlyList<GameRoundTeamOption>> GetEligibleTeamsAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetEligibleTeamsAsync(cancellationToken);
    }

    public Task<GameRoundDetails?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetActiveAsync(cancellationToken);
    }

    public Task<StartGameRoundResult> StartAsync(
        StartGameRoundInput input,
        Guid startedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.StartAsync(input, startedByUserId, cancellationToken)
        );
    }

    public Task<TransitionGameRoundResult> ReviewAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.ReviewAsync(roundId, input, reviewedByUserId, cancellationToken)
        );
    }

    public Task<TransitionGameRoundResult> PrepareAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.PrepareAsync(roundId, input, initiatedByUserId, cancellationToken)
        );
    }

    public Task<TransitionGameRoundResult> BeginGameplayAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.BeginGameplayAsync(roundId, input, initiatedByUserId, cancellationToken)
        );
    }

    public Task<TransitionGameRoundResult> ResumeGameplayAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.ResumeGameplayAsync(roundId, input, initiatedByUserId, cancellationToken)
        );
    }

    public Task<TransitionGameRoundResult> RebuildAsync(
        Guid roundId,
        GameRoundVersionCommandInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.RebuildAsync(roundId, input, initiatedByUserId, cancellationToken)
        );
    }

    public Task<TransitionGameRoundResult> TechnicalCancelAsync(
        Guid roundId,
        TechnicalCancelGameRoundInput input,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.TechnicalCancelAsync(roundId, input, initiatedByUserId, cancellationToken)
        );
    }

    public Task<FinalizeGameRoundResult> FinalizeAsync(
        Guid roundId,
        FinalizeGameRoundInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.FinalizeAsync(roundId, input, resolvedByUserId, cancellationToken)
        );
    }

    public Task<PreviewGameRoundScoreResult> PreviewScoreAsync(
        Guid roundId,
        FinalizeGameRoundInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.PreviewScoreAsync(roundId, input, resolvedByUserId, cancellationToken);
    }

    private async Task<T> PublishRoundStateChangeOnSuccessAsync<T>(Func<Task<T>> action)
        where T : class
    {
        var result = await action();
        var round = result switch
        {
            StartGameRoundResult start when start.Round is not null => start.Round,
            TransitionGameRoundResult transition when transition.Round is not null => transition.Round,
            FinalizeGameRoundResult finalize when finalize.Round is not null => finalize.Round,
            _ => null
        };

        if (round is null)
        {
            return result;
        }

        await RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishRoundStateChangedAsync(
                new GameRoundStateChangedEvent(
                    round.GameId,
                    round.RoundId,
                    round.Status,
                    round.RoundVersion,
                    DateTime.UtcNow
                ),
                publishToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameRoundStateChangedPublishFailed,
            round.RoundId
        );

        return result;
    }
}
