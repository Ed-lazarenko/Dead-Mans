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
                .Select(cell => MapCell(cell, mediaByCellId))
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
                selectedBoard.Version,
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

    public async Task<OpenGameCellResult?> TryOpenCellAsync(
        Guid cellId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (!_dbContext.Database.IsRelational())
            {
                return await TryOpenCellWithTrackedEntitiesAsync(cellId, cancellationToken);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var cell = await _dbContext.BoardCells
                .AsNoTracking()
                .Where(x => x.Id == cellId)
                .Select(x => new OpenCellTarget(
                    x.Id,
                    x.BoardId,
                    x.RowIndex,
                    x.ColIndex,
                    x.CellType,
                    x.Title,
                    x.Description,
                    x.Cost
                ))
                .FirstOrDefaultAsync(cancellationToken);
            if (cell is null)
            {
                _logger.LogInformation(AppMessages.Logs.GameCellNotFoundForOpen, cellId);
                return null;
            }

            var stateChanged =
                await _dbContext.BoardCells
                    .Where(x => x.Id == cellId && x.State != BoardCellState.Open)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.State, BoardCellState.Open),
                        cancellationToken
                    ) > 0;

            if (stateChanged)
            {
                await _dbContext.GameBoards
                    .Where(x => x.Id == cell.BoardId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(x => x.Version, x => x.Version + 1),
                        cancellationToken
                    );
                _logger.LogInformation(AppMessages.Logs.GameCellOpened, cellId);
            }
            else
            {
                _logger.LogInformation(AppMessages.Logs.GameCellAlreadyOpen, cellId);
            }

            var board = await _dbContext.GameBoards
                .AsNoTracking()
                .Where(x => x.Id == cell.BoardId)
                .Select(x => new { x.GameId, x.Version })
                .FirstAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var mappedCell = await LoadOpenedCellAsync(
                new RawCell(
                    cell.Id,
                    cell.Row,
                    cell.Col,
                    cell.CellType,
                    cell.Title,
                    cell.Description,
                    cell.Cost,
                    BoardCellState.Open
                ),
                cancellationToken
            );

            return new OpenGameCellResult(
                board.GameId.ToString(),
                board.Version,
                mappedCell,
                stateChanged
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameCellOpenFailed, cellId);
            throw;
        }
    }

    private async Task<OpenGameCellResult?> TryOpenCellWithTrackedEntitiesAsync(
        Guid cellId,
        CancellationToken cancellationToken
    )
    {
        var cell = await _dbContext.BoardCells.FirstOrDefaultAsync(
            x => x.Id == cellId,
            cancellationToken
        );
        if (cell is null)
        {
            _logger.LogInformation(AppMessages.Logs.GameCellNotFoundForOpen, cellId);
            return null;
        }

        var board = await _dbContext.GameBoards.FirstAsync(
            x => x.Id == cell.BoardId,
            cancellationToken
        );
        var stateChanged = false;
        if (cell.State == BoardCellState.Open)
        {
            _logger.LogInformation(AppMessages.Logs.GameCellAlreadyOpen, cellId);
        }
        else
        {
            cell.State = BoardCellState.Open;
            board.Version += 1;
            await _dbContext.SaveChangesAsync(cancellationToken);
            stateChanged = true;
            _logger.LogInformation(AppMessages.Logs.GameCellOpened, cellId);
        }

        var mappedCell = await LoadOpenedCellAsync(
            new RawCell(
                cell.Id,
                cell.RowIndex,
                cell.ColIndex,
                cell.CellType,
                cell.Title,
                cell.Description,
                cell.Cost,
                BoardCellState.Open
            ),
            cancellationToken
        );

        return new OpenGameCellResult(
            board.GameId.ToString(),
            board.Version,
            mappedCell,
            stateChanged
        );
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

    private async Task<GameBoardCell> LoadOpenedCellAsync(RawCell cell, CancellationToken cancellationToken)
    {
        var mediaByCellId = await LoadMediaByCellIdAsync([cell.Id], cancellationToken);
        return MapCell(
            new RawCell(cell.Id, cell.Row, cell.Col, cell.CellType, cell.Title, cell.Description, cell.Cost, BoardCellState.Open),
            mediaByCellId
        );
    }

    private static GameBoardCell MapCell(
        RawCell cell,
        IReadOnlyDictionary<Guid, List<GameBoardCellMedia>> mediaByCellId
    )
    {
        var state = cell.State == BoardCellState.Open ? GameBoardCellState.Open : GameBoardCellState.Closed;
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
                    board.Version,
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
                        row.Version,
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
                    row.Version,
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

    private sealed record OpenCellTarget(
        Guid Id,
        Guid BoardId,
        int Row,
        int Col,
        string CellType,
        string? Title,
        string? Description,
        int Cost
    );

    private sealed record SelectedBoard(
        Guid BoardId,
        Guid GameId,
        string Title,
        string? Description,
        string Status,
        int Version,
        int Rows,
        int Cols,
        string[] RowLabels,
        string[] ColLabels,
        DateTime ActiveSortAtUtc,
        DateTime FinishedSortAtUtc
    );
}
