using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameSetupRepository
{
    public async Task<GameBoardSnapshot?> GetLatestDraftSetupSnapshotAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var selected = await QueryDraftBoards()
                .Select(row => row.Board)
                .FirstOrDefaultAsync(cancellationToken);
            if (selected is null)
            {
                return null;
            }

            return await BuildSnapshotAsync(selected, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameSetupDraftLoadFailed);
            throw;
        }
    }

    public Task<bool> DraftGameExistsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Games
            .AsNoTracking()
            .AnyAsync(
                game => game.Status == GameStatusValue.Draft && !game.IsDeleted,
                cancellationToken
            );
    }

    private async Task<GameBoardSnapshot> BuildSnapshotAsync(
        SelectedBoard board,
        CancellationToken cancellationToken
    )
    {
        var rawCells = await _dbContext.BoardCells
            .AsNoTracking()
            .Where(cell => cell.BoardId == board.BoardId)
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

        var mediaByCellId = await GameBoardCellProjection.LoadMediaByCellIdAsync(
            _dbContext,
            _storagePublicBaseUrl,
            rawCells.Select(cell => cell.Id).ToArray(),
            cancellationToken
        );
        var resultCells = rawCells
            .Select(cell => GameBoardCellProjection.MapCell(cell, mediaByCellId, revealClosedContent: true))
            .ToArray();
        var enabledModifierIds = await _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .Where(x => x.GameId == board.GameId)
            .OrderBy(x => x.ModifierId)
            .Select(x => x.ModifierId)
            .ToArrayAsync(cancellationToken);
        var activeModifiers = await _dbContext.GameModifierActivations
            .AsNoTracking()
            .Where(
                x =>
                    x.GameId == board.GameId
                    && x.Status != GameModifierActivationStatusValue.Cancelled
            )
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
        var enabledQuestionIds = await _dbContext.GameEnabledQuestions
            .AsNoTracking()
            .Where(x => x.GameId == board.GameId)
            .OrderBy(x => x.QuestionId)
            .Select(x => x.QuestionId.ToString())
            .ToArrayAsync(cancellationToken);

        return new GameBoardSnapshot(
            board.GameId.ToString(),
            board.Title,
            board.Description,
            board.Status,
            board.Version,
            board.Rows,
            board.Cols,
            board.RowLabels,
            board.ColLabels,
            resultCells,
            enabledModifierIds,
            activeModifiers,
            null,
            enabledQuestionIds
        );
    }

    private static bool IsSingleDraftUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && postgresException.ConstraintName == SingleDraftConstraintName;
    }

    private IQueryable<BoardSelectionRow> QueryDraftBoards()
    {
        return _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatusValue.Draft && !game.IsDeleted)
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
                    game.CreatedAtUtc
                }
            )
            .OrderByDescending(row => row.CreatedAtUtc)
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
                    row.ColLabels
                )
            ));
    }

    private sealed record BoardSelectionRow(SelectedBoard Board);

    private sealed record QuestionSnapshotData(
        Guid Id,
        int Revision,
        string ExternalCode,
        string CategoryName,
        string Text,
        string[] AcceptedAnswers,
        string[] NormalizedAnswers,
        int Reward,
        int Priority
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
        string[] ColLabels
    );
}
