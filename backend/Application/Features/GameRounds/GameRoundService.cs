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
            () => _repository.StartAsync(input, startedByUserId, cancellationToken),
            cancellationToken
        );
    }

    public Task<ReviewGameRoundResult> ReviewAsync(
        Guid roundId,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishRoundStateChangeOnSuccessAsync(
            () => _repository.ReviewAsync(roundId, reviewedByUserId, cancellationToken),
            cancellationToken
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
            () => _repository.FinalizeAsync(roundId, input, resolvedByUserId, cancellationToken),
            cancellationToken
        );
    }

    private async Task<T> PublishRoundStateChangeOnSuccessAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var result = await action();
        var round = result switch
        {
            StartGameRoundResult start when start.Round is not null => start.Round,
            ReviewGameRoundResult review when review.Round is not null => review.Round,
            FinalizeGameRoundResult finalize when finalize.Round is not null => finalize.Round,
            _ => null
        };

        if (round is null)
        {
            return result;
        }

        await RealtimePublishGuard.TryPublishAsync(
            () => _eventsPublisher.PublishRoundStateChangedAsync(
                new GameRoundStateChangedEvent(
                    round.GameId,
                    round.RoundId,
                    round.Status,
                    DateTime.UtcNow
                ),
                cancellationToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameRoundStateChangedPublishFailed,
            round.RoundId
        );

        return result;
    }
}
