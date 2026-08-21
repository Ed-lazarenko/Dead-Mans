using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

internal static class GameBoardCellProjection
{
    internal sealed record RawCell(
        Guid Id,
        int Row,
        int Col,
        string CellType,
        string? Title,
        string? Description,
        int Cost,
        BoardCellState State
    );

    internal static async Task<Dictionary<Guid, List<GameBoardCellMedia>>> LoadMediaByCellIdAsync(
        ApplicationDbContext dbContext,
        string storagePublicBaseUrl,
        Guid[] cellIds,
        CancellationToken cancellationToken
    )
    {
        if (cellIds.Length == 0)
        {
            return [];
        }

        var mediaRows = await dbContext.BoardCellMedia
            .AsNoTracking()
            .Where(link => cellIds.Contains(link.CellId))
            .OrderBy(link => link.SortOrder)
            .Select(link => new { link.CellId, link.MediaAsset.Bucket, link.MediaAsset.ObjectKey })
            .ToListAsync(cancellationToken);

        return mediaRows
            .GroupBy(row => row.CellId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new GameBoardCellMedia(
                        GameBoardMediaUrlBuilder.Build(storagePublicBaseUrl, item.Bucket, item.ObjectKey)
                    ))
                    .ToList()
            );
    }

    internal static GameBoardCell MapCell(
        RawCell cell,
        IReadOnlyDictionary<Guid, List<GameBoardCellMedia>> mediaByCellId,
        bool revealClosedContent
    )
    {
        var state = cell.State switch
        {
            BoardCellState.Open => GameBoardCellState.Open,
            BoardCellState.Cancelled => GameBoardCellState.Cancelled,
            _ => GameBoardCellState.Closed
        };
        IReadOnlyList<GameBoardCellMedia> media;
        if (revealClosedContent)
        {
            media = mediaByCellId.TryGetValue(cell.Id, out var allMedia) ? allMedia : [];
        }
        else
        {
            media = state != GameBoardCellState.Closed
                && mediaByCellId.TryGetValue(cell.Id, out var openMedia)
                ? openMedia
                : [];
        }

        var revealContent = revealClosedContent || state != GameBoardCellState.Closed;

        return new GameBoardCell(
            cell.Id.ToString(),
            cell.Row,
            cell.Col,
            cell.CellType,
            revealContent ? cell.Title : null,
            revealContent ? cell.Description : null,
            cell.Cost,
            state,
            media
        );
    }
}
