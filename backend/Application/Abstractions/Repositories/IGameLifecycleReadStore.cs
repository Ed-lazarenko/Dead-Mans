using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public interface IGameLifecycleReadStore
{
    Task<DraftGameLifecycleContext?> GetLatestDraftForOpenAsync(
        CancellationToken cancellationToken = default
    );

    Task<bool> AnyReadyGameAsync(CancellationToken cancellationToken = default);

    Task<bool> AnyActiveGameAsync(CancellationToken cancellationToken = default);

    Task<Guid?> GetReadyGameIdForStartAsync(CancellationToken cancellationToken = default);

    Task<GameLifecycleErrorCode> GetStartValidationErrorAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

}
