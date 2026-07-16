using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;

namespace backend.Application.Features.GameLifecycle;

public sealed class GameLifecycleService : IGameLifecycleService
{
    private readonly IGameLifecycleReadStore _reads;
    private readonly IGameLifecyclePersistence _persistence;

    public GameLifecycleService(IGameLifecycleReadStore reads, IGameLifecyclePersistence persistence)
    {
        _reads = reads;
        _persistence = persistence;
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

        if (await _reads.AnyReadyGameAsync(cancellationToken))
        {
            return new GameLifecycleResult(
                false,
                draft.GameId,
                GameLifecycleErrorCode.ReadyGameAlreadyExists
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

    public async Task<GameLifecycleResult> FinishGameAsync(CancellationToken cancellationToken = default)
    {
        var activeGameId = await _reads.GetActiveGameIdForFinishAsync(cancellationToken);
        if (activeGameId is null)
        {
            return new GameLifecycleResult(false, null, GameLifecycleErrorCode.GameNotActive);
        }

        return await _persistence.FinishGameAsync(activeGameId.Value, cancellationToken);
    }

    public Task<GameLifecycleResult> ArchiveGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return _persistence.ArchiveGameAsync(gameId, cancellationToken);
    }
}
