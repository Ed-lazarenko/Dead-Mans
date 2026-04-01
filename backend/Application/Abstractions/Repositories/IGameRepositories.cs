using backend.Domain.Models;

namespace backend.Application.Abstractions.Repositories;

public interface IGameBoardRepository
{
    Task<backend.Application.Contracts.GameBoardSnapshot?> GetCurrentBoardAsync(
        CancellationToken cancellationToken = default
    );
}
