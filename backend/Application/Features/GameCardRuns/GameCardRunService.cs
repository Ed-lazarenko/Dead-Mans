using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Messaging;

namespace backend.Application.Features.GameCardRuns;

public sealed class GameCardRunService : IGameCardRunService
{
    private readonly IGameCardRunRepository _repository;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameCardRunService> _logger;

    public GameCardRunService(
        IGameCardRunRepository repository,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameCardRunService> logger
    )
    {
        _repository = repository;
        _eventsPublisher = eventsPublisher;
        _logger = logger;
    }

    public Task<IReadOnlyList<GameCardRunTeamOption>> GetEligibleTeamsAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetEligibleTeamsAsync(cancellationToken);
    }

    public Task<GameCardRunDetails?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetActiveAsync(cancellationToken);
    }

    public Task<StartGameCardRunResult> StartAsync(
        StartGameCardRunInput input,
        Guid startedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishCardRunStateChangeOnSuccessAsync(
            () => _repository.StartAsync(input, startedByUserId, cancellationToken),
            cancellationToken
        );
    }

    public Task<ReviewGameCardRunResult> ReviewAsync(
        Guid cardRunId,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishCardRunStateChangeOnSuccessAsync(
            () => _repository.ReviewAsync(cardRunId, reviewedByUserId, cancellationToken),
            cancellationToken
        );
    }

    public Task<FinalizeGameCardRunResult> FinalizeAsync(
        Guid cardRunId,
        FinalizeGameCardRunInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return PublishCardRunStateChangeOnSuccessAsync(
            () => _repository.FinalizeAsync(cardRunId, input, resolvedByUserId, cancellationToken),
            cancellationToken
        );
    }

    private async Task<T> PublishCardRunStateChangeOnSuccessAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var result = await action();
        var run = result switch
        {
            StartGameCardRunResult start when start.Run is not null => start.Run,
            ReviewGameCardRunResult review when review.Run is not null => review.Run,
            FinalizeGameCardRunResult finalize when finalize.Run is not null => finalize.Run,
            _ => null
        };

        if (run is null)
        {
            return result;
        }

        await RealtimePublishGuard.TryPublishAsync(
            () => _eventsPublisher.PublishCardRunStateChangedAsync(
                new GameCardRunStateChangedEvent(
                    run.GameId,
                    run.CardRunId,
                    run.Status,
                    DateTime.UtcNow
                ),
                cancellationToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameCardRunStateChangedPublishFailed,
            run.CardRunId
        );

        return result;
    }
}
