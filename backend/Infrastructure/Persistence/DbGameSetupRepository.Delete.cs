using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameSetupRepository
{
    public async Task<Guid?> DeleteDraftSetupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var draftGame = await _dbContext.Games
                .Where(game => game.Status == GameStatusValue.Draft && !game.IsDeleted)
                .OrderByDescending(game => game.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (draftGame is null)
            {
                return null;
            }

            var gameId = draftGame.Id;
            await RemoveDraftBoardMediaAsync(gameId, cancellationToken);
            _dbContext.Games.Remove(draftGame);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(AppMessages.Logs.GameSetupDraftDeleted, gameId);
            return gameId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameSetupDraftDeleteFailed);
            throw;
        }
    }

    private async Task RemoveDraftBoardMediaAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var boardId = await _dbContext.GameBoards
            .Where(board => board.GameId == gameId)
            .Select(board => (Guid?)board.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (boardId is null)
        {
            return;
        }

        var mediaLinks = await _dbContext.BoardCellMedia
            .Where(link =>
                _dbContext.BoardCells
                    .Where(cell => cell.BoardId == boardId)
                    .Select(cell => cell.Id)
                    .Contains(link.CellId)
            )
            .Include(link => link.MediaAsset)
            .ToListAsync(cancellationToken);
        if (mediaLinks.Count == 0)
        {
            return;
        }

        _dbContext.BoardCellMedia.RemoveRange(mediaLinks);
        _dbContext.MediaAssets.RemoveRange(
            mediaLinks.Select(link => link.MediaAsset).DistinctBy(asset => asset.Id)
        );
    }
}
