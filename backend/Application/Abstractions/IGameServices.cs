using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface IGameBoardService
{
    Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default);
}
