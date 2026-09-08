using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameBoardRepository
{
    public async Task<GameBoardSnapshot?> GetLatestBoardByStatusAsync(
        string status,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var selectedBoard = await QueryBoardsByStatus(
                    status,
                    useFinishedSort: status == GameStatusValue.Finished
                )
                .Select(x => x.Board)
                .FirstOrDefaultAsync(cancellationToken);
            if (selectedBoard is null)
            {
                _logger.LogDebug("No game board found for status {Status}.", status);
                return null;
            }

            var cells = await _dbContext.BoardCells
                .AsNoTracking()
                .Where(cell => cell.BoardId == selectedBoard.BoardId)
                .OrderBy(cell => cell.RowIndex)
                .ThenBy(cell => cell.ColIndex)
                .Select(cell => new GameBoardCellProjection.RawCell(
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
                .Where(cell => cell.State != BoardCellState.Closed)
                .Select(cell => cell.Id)
                .ToArray();

            var mediaByCellId = await GameBoardCellProjection.LoadMediaByCellIdAsync(
                _dbContext,
                _storagePublicBaseUrl,
                openCellIds,
                cancellationToken
            );

            var resultCells = cells
                .Select(cell => GameBoardCellProjection.MapCell(cell, mediaByCellId, revealClosedContent: false))
                .ToArray();
            var enabledModifierIds = await _dbContext.GameEnabledModifiers
                .AsNoTracking()
                .Where(x => x.GameId == selectedBoard.GameId)
                .OrderBy(x => x.ModifierId)
                .Select(x => x.ModifierId)
                .ToArrayAsync(cancellationToken);
            var activeModifiers = await _dbContext.GameModifierActivations
                .AsNoTracking()
                .Where(x => x.GameId == selectedBoard.GameId && x.ArchivedAtUtc == null)
                .OrderBy(x => x.ActivatedAtUtc)
                .Select(
                    x => new Application.Contracts.GameModifierActivation(
                        x.Id,
                        x.RoundId,
                        x.Round.Version,
                        x.ModifierId,
                        x.ModifierNameSnapshot,
                        x.ActivatedByUserId.ToString(),
                        x.ActivatedByUser != null ? x.ActivatedByUser.DisplayName : x.ActivatedByUserId.ToString(),
                        x.ActivationCostSnapshot,
                        x.ActivatedAtUtc
                    )
                )
                .ToArrayAsync(cancellationToken);

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
                resultCells,
                enabledModifierIds,
                activeModifiers,
                selectedBoard.ActiveTeamId?.ToString(),
                Array.Empty<string>()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.DbGameBoardLoadError);
            throw;
        }
    }

    private IQueryable<BoardSelectionRow> QueryBoardsByStatus(string status, bool useFinishedSort)
    {
        var baseQuery = _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == status && !game.IsDeleted)
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
                    game.ActiveTeamId,
                    board.Version,
                    board.Rows,
                    board.Cols,
                    board.RowLabels,
                    board.ColLabels,
                    ActiveSortAtUtc = status == GameStatusValue.Ready
                        ? game.ReadyAtUtc ?? game.CreatedAtUtc
                        : game.StartedAtUtc ?? game.CreatedAtUtc,
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
                        row.ActiveTeamId,
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
                    row.ActiveTeamId,
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

    private sealed record SelectedBoard(
        Guid BoardId,
        Guid GameId,
        string Title,
        string? Description,
        string Status,
        Guid? ActiveTeamId,
        int Version,
        int Rows,
        int Cols,
        string[] RowLabels,
        string[] ColLabels,
        DateTime ActiveSortAtUtc,
        DateTime FinishedSortAtUtc
    );
}
