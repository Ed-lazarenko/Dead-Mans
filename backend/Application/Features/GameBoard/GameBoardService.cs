using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;

namespace backend.Application.Features.GameBoard;

public sealed class GameBoardService : IGameBoardService
{
    private readonly IGameBoardRepository _repository;

    public GameBoardService(IGameBoardRepository repository)
    {
        _repository = repository;
    }

    public Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetCurrentBoardAsync(cancellationToken);
    }
}
