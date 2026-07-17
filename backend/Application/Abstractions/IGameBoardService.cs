using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface IGameBoardService
{
    Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameTeamQueueItem>> GetCurrentTeamQueueAsync(
        CancellationToken cancellationToken = default
    );
    Task<SetActiveGameTeamOutcome> SetCurrentActiveTeamAsync(
        Guid? teamId,
        CancellationToken cancellationToken = default
    );
    Task<bool> CurrentActiveGameHasSelectedTeamAsync(CancellationToken cancellationToken = default);
    Task<bool> IsCurrentActiveGameCellAsync(Guid cellId, CancellationToken cancellationToken = default);
    Task<OpenGameCellResult?> TryOpenCellAsync(Guid cellId, CancellationToken cancellationToken = default);
}
