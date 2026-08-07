using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface IGameBoardService
{
    Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameTeamQueueItem>> GetCurrentTeamQueueAsync(
        CancellationToken cancellationToken = default
    );
    Task<SetActiveGameTeamOutcome> SetActiveTeamAsync(
        Guid? teamId,
        CancellationToken cancellationToken = default
    );
    Task<SetGameTeamPlayedStateOutcome> SetGameTeamPlayedStateAsync(
        Guid teamId,
        bool isPlayed,
        CancellationToken cancellationToken = default
    );
    Task<bool> CurrentActiveGameHasActiveTeamAsync(CancellationToken cancellationToken = default);
    Task<bool> CurrentActiveGameHasActiveRoundAsync(CancellationToken cancellationToken = default);
    Task<bool> IsCurrentActiveGameCellAsync(Guid cellId, CancellationToken cancellationToken = default);
    Task<OpenGameCellResult?> TryOpenCellAsync(Guid cellId, CancellationToken cancellationToken = default);
}
