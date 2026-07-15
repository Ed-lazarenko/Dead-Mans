using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;

namespace backend.Application.Features.GameHistory;

public sealed class GameHistoryService : IGameHistoryService
{
    private readonly IGameHistoryRepository _repository;

    public GameHistoryService(IGameHistoryRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<GameHistoryLeaderboardEntry>> GetLeaderboardAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetLeaderboardAsync(cancellationToken);
    }

    public Task<IReadOnlyList<GameHistoryGameSummary>> GetGamesAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetGamesAsync(cancellationToken);
    }

    public Task<GameHistoryGameDetails?> GetGameDetailsAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetGameDetailsAsync(gameId, cancellationToken);
    }

    public Task<IReadOnlyList<UserGameHistoryItem>> GetUserGameHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetUserGameHistoryAsync(userId, cancellationToken);
    }
}
