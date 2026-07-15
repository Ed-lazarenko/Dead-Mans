using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface IGameHistoryService
{
    Task<IReadOnlyList<GameHistoryLeaderboardEntry>> GetLeaderboardAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<GameHistoryGameSummary>> GetGamesAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameHistoryGameDetails?> GetGameDetailsAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<UserGameHistoryItem>> GetUserGameHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
