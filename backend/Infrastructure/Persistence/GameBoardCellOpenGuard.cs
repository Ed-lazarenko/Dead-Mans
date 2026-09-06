using backend.Data;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

internal static class GameBoardCellOpenGuard
{
    internal sealed record OpenCellTarget(
        Guid Id,
        Guid BoardId,
        Guid GameId,
        int Row,
        int Col,
        string CellType,
        string? Title,
        string? Description,
        int Cost
    );

    public static Task<OpenCellTarget?> FindActiveGameCellAsync(
        ApplicationDbContext dbContext,
        Guid cellId,
        CancellationToken cancellationToken
    )
    {
        return (
            from cell in dbContext.BoardCells.AsNoTracking()
            join board in dbContext.GameBoards.AsNoTracking() on cell.BoardId equals board.Id
            join game in dbContext.Games.AsNoTracking() on board.GameId equals game.Id
            where cell.Id == cellId && game.Status == GameStatusValue.Active && !game.IsDeleted
            select new OpenCellTarget(
                cell.Id,
                cell.BoardId,
                game.Id,
                cell.RowIndex,
                cell.ColIndex,
                cell.CellType,
                cell.Title,
                cell.Description,
                cell.Cost
            )
        ).FirstOrDefaultAsync(cancellationToken);
    }
}
