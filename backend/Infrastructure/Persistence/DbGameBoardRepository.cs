using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using backend.Infrastructure.Configuration;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameBoardRepository : IGameBoardRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string _storagePublicBaseUrl;
    private readonly ILogger<DbGameBoardRepository> _logger;

    public DbGameBoardRepository(
        ApplicationDbContext dbContext,
        IOptions<StorageOptions> storageOptions,
        ILogger<DbGameBoardRepository> logger
    )
    {
        _dbContext = dbContext;
        _storagePublicBaseUrl = storageOptions.Value.PublicBaseUrl.TrimEnd('/');
        _logger = logger;
    }

    public async Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var selectedBoard = await SelectCurrentBoardAsync(cancellationToken);
            if (selectedBoard is null)
            {
                _logger.LogDebug(AppMessages.Logs.NoActiveOrFinishedGameRow);
                return null;
            }

            var cells = await _dbContext.BoardCells
                .AsNoTracking()
                .Where(cell => cell.BoardId == selectedBoard.BoardId)
                .OrderBy(cell => cell.RowIndex)
                .ThenBy(cell => cell.ColIndex)
                .Select(cell => new RawCell(
                    cell.Id,
                    cell.RowIndex,
                    cell.ColIndex,
                    cell.CellType,
                    cell.Title,
                    cell.Description,
                    cell.Cost,
                    cell.State
                ))
                .ToListAsync(cancellationToken);

            var openCellIds = cells
                .Where(cell => cell.State == BoardCellState.Open)
                .Select(cell => cell.Id)
                .ToArray();

            var mediaByCellId = await LoadMediaByCellIdAsync(openCellIds, cancellationToken);

            var resultCells = cells
                .Select(cell =>
                {
                    var state = cell.State == BoardCellState.Open
                        ? GameBoardCellState.Open
                        : GameBoardCellState.Closed;

                    var media = state == GameBoardCellState.Open
                        && mediaByCellId.TryGetValue(cell.Id, out var cellMedia)
                        ? cellMedia
                        : [];

                    return new GameBoardCell(
                        cell.Id.ToString(),
                        cell.Row,
                        cell.Col,
                        cell.CellType,
                        cell.Title,
                        cell.Description,
                        cell.Cost,
                        state,
                        media
                    );
                })
                .ToArray();

            _logger.LogDebug(
                AppMessages.Logs.GameBoardSnapshotResolved,
                selectedBoard.GameId,
                selectedBoard.Status,
                resultCells.Length
            );

            return new GameBoardSnapshot(
                selectedBoard.GameId.ToString(),
                selectedBoard.Title,
                selectedBoard.Description,
                selectedBoard.Status,
                selectedBoard.Rows,
                selectedBoard.Cols,
                selectedBoard.RowLabels,
                selectedBoard.ColLabels,
                resultCells
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.DbGameBoardLoadError);
            throw;
        }
    }

    private async Task<SelectedBoard?> SelectCurrentBoardAsync(CancellationToken cancellationToken)
    {
        var activeBoard = await QueryBoardsByStatus(GameStatusValue.Active, useFinishedSort: false)
            .Select(x => x.Board)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeBoard is not null)
        {
            return activeBoard;
        }

        return await QueryBoardsByStatus(GameStatusValue.Finished, useFinishedSort: true)
            .Select(x => x.Board)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, List<GameBoardCellMedia>>> LoadMediaByCellIdAsync(
        Guid[] openCellIds,
        CancellationToken cancellationToken
    )
    {
        if (openCellIds.Length == 0)
        {
            return [];
        }

        var mediaRows = await _dbContext.BoardCellMedia
            .AsNoTracking()
            .Where(link => openCellIds.Contains(link.CellId))
            .OrderBy(link => link.SortOrder)
            .Select(link => new { link.CellId, link.MediaAsset.Bucket, link.MediaAsset.ObjectKey })
            .ToListAsync(cancellationToken);

        return mediaRows
            .GroupBy(row => row.CellId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => new GameBoardCellMedia(BuildPublicMediaUrl(item.Bucket, item.ObjectKey)))
                    .ToList()
            );
    }

    private string BuildPublicMediaUrl(string bucket, string objectKey)
    {
        return $"{_storagePublicBaseUrl}/{bucket}/{objectKey}";
    }

    private IQueryable<BoardSelectionRow> QueryBoardsByStatus(string status, bool useFinishedSort)
    {
        var baseQuery = _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == status)
            .Join(
                _dbContext.GameBoards.AsNoTracking(),
                game => game.Id,
                board => board.GameId,
                (game, board) => new
                {
                    board.Id,
                    GameId = game.Id,
                    game.Title,
                    game.Description,
                    game.Status,
                    board.Rows,
                    board.Cols,
                    board.RowLabels,
                    board.ColLabels,
                    ActiveSortAtUtc = game.StartedAtUtc ?? game.CreatedAtUtc,
                    FinishedSortAtUtc = game.FinishedAtUtc ?? game.CreatedAtUtc
                }
            );

        if (useFinishedSort)
        {
            return baseQuery
                .OrderByDescending(row => row.FinishedSortAtUtc)
                .Select(row => new BoardSelectionRow(
                    new SelectedBoard(
                        row.Id,
                        row.GameId,
                        row.Title,
                        row.Description,
                        row.Status,
                        row.Rows,
                        row.Cols,
                        row.RowLabels,
                        row.ColLabels,
                        row.ActiveSortAtUtc,
                        row.FinishedSortAtUtc
                    )
                ));
        }

        return baseQuery
            .OrderByDescending(row => row.ActiveSortAtUtc)
            .Select(row => new BoardSelectionRow(
                new SelectedBoard(
                    row.Id,
                    row.GameId,
                    row.Title,
                    row.Description,
                    row.Status,
                    row.Rows,
                    row.Cols,
                    row.RowLabels,
                    row.ColLabels,
                    row.ActiveSortAtUtc,
                    row.FinishedSortAtUtc
                )
            ));
    }

    private sealed record BoardSelectionRow(SelectedBoard Board);

    private sealed record RawCell(
        Guid Id,
        int Row,
        int Col,
        string CellType,
        string? Title,
        string? Description,
        int Cost,
        BoardCellState State
    );

    private sealed record SelectedBoard(
        Guid BoardId,
        Guid GameId,
        string Title,
        string? Description,
        string Status,
        int Rows,
        int Cols,
        string[] RowLabels,
        string[] ColLabels,
        DateTime ActiveSortAtUtc,
        DateTime FinishedSortAtUtc
    );
}
