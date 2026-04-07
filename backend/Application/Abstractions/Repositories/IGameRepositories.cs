using backend.Domain.Models;
using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public interface IGameBoardRepository
{
    Task<backend.Application.Contracts.GameBoardSnapshot?> GetCurrentBoardAsync(
        CancellationToken cancellationToken = default
    );

    Task<OpenGameCellResult?> TryOpenCellAsync(Guid cellId, CancellationToken cancellationToken = default);
}
