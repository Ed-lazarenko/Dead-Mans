using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;

namespace backend.Application.Features.Loadout;

public sealed class LoadoutService : ILoadoutService
{
    private readonly ILoadoutRepository _repository;

    public LoadoutService(ILoadoutRepository repository)
    {
        _repository = repository;
    }

    public async Task<Contracts.LoadoutBoard> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var board = await _repository.GetBoardAsync(cancellationToken);

        return new Contracts.LoadoutBoard(
            board.Rows,
            board.Cols,
            board.RowLabels.ToArray(),
            board.ColLabels.ToArray(),
            board.Cells
                .Select(cell => new Contracts.LoadoutCell(
                    cell.Id,
                    cell.Row,
                    cell.Col,
                    cell.Label,
                    cell.Points,
                    cell.ImageUrl,
                    cell.State
                ))
                .ToArray()
        );
    }
}
