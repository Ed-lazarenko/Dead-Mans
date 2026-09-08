using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameSetupCellMediaRepository : IGameSetupCellMediaRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DbGameSetupCellMediaRepository(
        ApplicationDbContext dbContext,
        TimeProvider timeProvider
    )
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<GameSetupDraftCellRef?> FindDraftCellAsync(
        Guid cellId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.BoardCells
            .AsNoTracking()
            .Where(cell => cell.Id == cellId)
            .Join(
                _dbContext.GameBoards.AsNoTracking(),
                cell => cell.BoardId,
                board => board.Id,
                (cell, board) => new { cell, board }
            )
            .Join(
                _dbContext.Games.AsNoTracking(),
                row => row.board.GameId,
                game => game.Id,
                (row, game) => new { row.cell, row.board, game }
            )
            .Where(row => row.game.Status == GameStatusValue.Draft && !row.game.IsDeleted)
            .Select(row => new GameSetupDraftCellRef(
                row.game.Id,
                row.board.Id,
                row.cell.Id,
                row.cell.RowIndex,
                row.cell.ColIndex
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StoredCellMedia?> GetCellMediaAsync(
        Guid cellId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.BoardCellMedia
            .AsNoTracking()
            .Where(link => link.CellId == cellId)
            .OrderBy(link => link.SortOrder)
            .Select(link => new StoredCellMedia(
                link.MediaAssetId,
                link.MediaAsset.Bucket,
                link.MediaAsset.ObjectKey
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GameBoardCellMedia> AttachMediaAsync(
        Guid cellId,
        Guid mediaAssetId,
        string bucket,
        string objectKey,
        string mimeType,
        long sizeBytes,
        string publicBaseUrl,
        CancellationToken cancellationToken = default
    )
    {
        var existingLinks = await _dbContext.BoardCellMedia
            .Where(link => link.CellId == cellId)
            .Include(link => link.MediaAsset)
            .ToListAsync(cancellationToken);

        if (existingLinks.Count > 0)
        {
            var existingAssets = existingLinks.Select(link => link.MediaAsset).ToList();
            _dbContext.BoardCellMedia.RemoveRange(existingLinks);
            _dbContext.MediaAssets.RemoveRange(existingAssets);
        }

        var mediaAsset = new MediaAsset
        {
            Id = mediaAssetId,
            Bucket = bucket,
            ObjectKey = objectKey,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };

        var link = new BoardCellMedia
        {
            Id = Guid.NewGuid(),
            CellId = cellId,
            MediaAssetId = mediaAsset.Id,
            SortOrder = 0,
        };

        _dbContext.MediaAssets.Add(mediaAsset);
        _dbContext.BoardCellMedia.Add(link);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GameBoardCellMedia(GameBoardMediaUrlBuilder.Build(publicBaseUrl, bucket, objectKey));
    }

    public async Task<StoredCellMedia?> DetachMediaAsync(
        Guid cellId,
        CancellationToken cancellationToken = default
    )
    {
        var existingLinks = await _dbContext.BoardCellMedia
            .Where(link => link.CellId == cellId)
            .Include(link => link.MediaAsset)
            .ToListAsync(cancellationToken);
        if (existingLinks.Count == 0)
        {
            return null;
        }

        var first = existingLinks[0];
        var stored = new StoredCellMedia(first.MediaAssetId, first.MediaAsset.Bucket, first.MediaAsset.ObjectKey);
        var assets = existingLinks.Select(link => link.MediaAsset).ToList();
        _dbContext.BoardCellMedia.RemoveRange(existingLinks);
        _dbContext.MediaAssets.RemoveRange(assets);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return stored;
    }

}
