using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using backend.Application.Realtime;

namespace backend.Application.Features.GameLifecycle;

public sealed class GameLifecycleService : IGameLifecycleService
{
    private readonly IGameLifecycleReadStore _reads;
    private readonly IGameLifecyclePersistence _persistence;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameLifecycleService> _logger;

    public GameLifecycleService(
        IGameLifecycleReadStore reads,
        IGameLifecyclePersistence persistence,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameLifecycleService> logger
    )
    {
        _reads = reads;
        _persistence = persistence;
        _eventsPublisher = eventsPublisher;
        _logger = logger;
    }

    public async Task<GameLifecycleResult> OpenRegistrationAsync(CancellationToken cancellationToken = default)
    {
        var draft = await _reads.GetLatestDraftForOpenAsync(cancellationToken);
        if (draft is null)
        {
            return new GameLifecycleResult(false, null, GameLifecycleErrorCode.DraftNotFound);
        }

        if (draft.MinPlayersPerTeam > draft.MaxPlayersPerTeam)
        {
            return new GameLifecycleResult(
                false,
                draft.GameId,
                GameLifecycleErrorCode.InvalidTeamSizeLimits
            );
        }

        if (await _reads.AnyReadyGameAsync(cancellationToken)
            || await _reads.AnyActiveGameAsync(cancellationToken))
        {
            return new GameLifecycleResult(
                false,
                draft.GameId,
                GameLifecycleErrorCode.CurrentGameAlreadyExists
            );
        }

        return await _persistence.OpenRegistrationAsync(draft.GameId, cancellationToken);
    }

    public async Task<GameLifecycleResult> StartGameAsync(CancellationToken cancellationToken = default)
    {
        var readyGameId = await _reads.GetReadyGameIdForStartAsync(cancellationToken);
        if (readyGameId is null)
        {
            return new GameLifecycleResult(false, null, GameLifecycleErrorCode.GameNotReady);
        }

        if (await _reads.AnyActiveGameAsync(cancellationToken))
        {
            return new GameLifecycleResult(
                false,
                readyGameId,
                GameLifecycleErrorCode.ActiveGameAlreadyExists
            );
        }

        var startValidationError = await _reads.GetStartValidationErrorAsync(
            readyGameId.Value,
            cancellationToken
        );
        if (startValidationError != GameLifecycleErrorCode.None)
        {
            return new GameLifecycleResult(false, readyGameId, startValidationError);
        }

        return await _persistence.StartGameAsync(readyGameId.Value, cancellationToken);
    }

    public Task<GameFinishPreviewResult> GetFinishPreviewAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return _persistence.GetFinishPreviewAsync(gameId, cancellationToken);
    }

    public async Task<FinishGameResult> FinishGameAsync(
        Guid gameId,
        FinishGameInput input,
        Guid finishedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (input.ExpectedBoardVersion < 1
            || input.RequestId == Guid.Empty
            || input.PublicNote?.Length > 2000)
        {
            return new FinishGameResult(GameLifecycleErrorCode.FinishInvalidRequest, null);
        }

        var result = await _persistence.FinishGameAsync(
            gameId,
            input,
            finishedByUserId,
            cancellationToken
        );
        if (!result.Success || result.AlreadyFinished || result.Summary is null)
        {
            return result;
        }

        var summary = result.Summary;
        await RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishGameLifecycleChangedAsync(
                new GameLifecycleChangedEvent(
                    summary.GameId,
                    summary.GameStatus,
                    summary.BoardVersion,
                    summary.FinishedAtUtc ?? DateTime.UtcNow
                ),
                publishToken
            ),
            _logger,
            "Realtime game lifecycle publish failed for game {GameId}.",
            summary.GameId
        );

        return result;
    }

    public Task<GameLifecycleResult> ArchiveGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return _persistence.ArchiveGameAsync(gameId, cancellationToken);
    }
}
