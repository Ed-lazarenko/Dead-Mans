using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public interface IGameLifecyclePersistence
{
    Task<GameLifecycleResult> OpenRegistrationAsync(
        Guid draftGameId,
        CancellationToken cancellationToken = default
    );

    Task<GameLifecycleResult> StartGameAsync(
        Guid readyGameId,
        CancellationToken cancellationToken = default
    );

    Task<GameFinishPreviewResult> GetFinishPreviewAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<FinishGameResult> FinishGameAsync(
        Guid gameId,
        FinishGameInput input,
        Guid finishedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<GameLifecycleResult> ArchiveGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );
}
