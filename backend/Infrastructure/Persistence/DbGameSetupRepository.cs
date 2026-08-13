using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Configuration;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using backend.Infrastructure.Configuration;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameSetupRepository : IGameSetupRepository
{
    private const string SingleDraftConstraintName = "ux_games_single_draft";

    private readonly ApplicationDbContext _dbContext;
    private readonly string _storagePublicBaseUrl;
    private readonly ILogger<DbGameSetupRepository> _logger;

    public DbGameSetupRepository(
        ApplicationDbContext dbContext,
        IOptions<StorageOptions> storageOptions,
        ILogger<DbGameSetupRepository> logger
    )
    {
        _dbContext = dbContext;
        _storagePublicBaseUrl = storageOptions.Value.PublicBaseUrl.TrimEnd('/');
        _logger = logger;
    }

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

    public async Task<GameBoardSnapshot?> CreateDraftSetupAsync(
        string title,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            var gameId = Guid.NewGuid();
            var boardId = Guid.NewGuid();
            var rowLabels = GameSetupDefaults.BuildRowLabels();
            var columnLabels = GameSetupDefaults.BuildColumnLabels();

            var game = new Game
            {
                Id = gameId,
                Title = title,
                Description = null,
                Status = GameStatusValue.Draft,
                CreatedAtUtc = utcNow,
                ReadyAtUtc = null,
                StartedAtUtc = null,
                FinishedAtUtc = null,
                MinPlayersPerTeam = GameRegistrationDefaults.MinPlayersPerTeam,
                MaxPlayersPerTeam = GameRegistrationDefaults.MaxPlayersPerTeam
            };

            var teamSlots = GameRegistrationDefaults
                .BuildDefaultTeamSlots()
                .Select(
                    slot =>
                        new GameTeamSlot
                        {
                            Id = Guid.NewGuid(),
                            GameId = gameId,
                            SlotIndex = slot.TeamSlotIndex,
                            SlotType = slot.TeamSlotType,
                            ReservedLabel = null,
                            CreatedAtUtc = utcNow
                        }
                )
                .ToList();

            var board = new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Version = 1,
                Rows = GameSetupDefaults.Rows,
                Cols = GameSetupDefaults.Cols,
                RowLabels = rowLabels,
                ColLabels = columnLabels,
                CreatedAtUtc = utcNow
            };

            var cells = new List<BoardCell>(GameSetupDefaults.Rows * GameSetupDefaults.Cols);
            for (var row = 0; row < GameSetupDefaults.Rows; row += 1)
            {
                for (var col = 0; col < GameSetupDefaults.Cols; col += 1)
                {
                    cells.Add(
                        new BoardCell
                        {
                            Id = Guid.NewGuid(),
                            BoardId = boardId,
                            RowIndex = row,
                            ColIndex = col,
                            State = BoardCellState.Closed,
                            CellType = BoardCellPersistence.DefaultCellType,
                            Title = null,
                            Cost = GameSetupDefaults.GetRowCost(row),
                            Description = null
                        }
                    );
                }
            }

            _dbContext.Games.Add(game);
            _dbContext.GameTeamSlots.AddRange(teamSlots);
            _dbContext.GameBoards.Add(board);
            _dbContext.BoardCells.AddRange(cells);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                AppMessages.Logs.GameSetupDraftCreated,
                gameId,
                cells.Count
            );

            return await BuildSnapshotAsync(
                new SelectedBoard(
                    boardId,
                    gameId,
                    title,
                    null,
                    GameStatusValue.Draft,
                    board.Version,
                    board.Rows,
                    board.Cols,
                    board.RowLabels,
                    board.ColLabels
                ),
                cancellationToken
            );
        }
        catch (DbUpdateException ex) when (IsSingleDraftUniqueViolation(ex))
        {
            _logger.LogInformation(AppMessages.Logs.GameSetupDraftAlreadyExists);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameSetupDraftCreateFailed);
            throw;
        }
    }

    public async Task<UpdateDraftSetupRepositoryResult> UpdateDraftSetupAsync(
        GameSetupDraftUpdate update,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var draftGame = await _dbContext.Games
                .Include(game => game.Board!)
                .ThenInclude(board => board.Cells)
                .Where(game => game.Status == GameStatusValue.Draft && !game.IsDeleted)
                .OrderByDescending(game => game.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (draftGame?.Board is not { } board)
            {
                return new UpdateDraftSetupRepositoryResult(UpdateDraftSetupRepositoryStatus.NotFound);
            }

            if (board.Version != update.ExpectedVersion)
            {
                _logger.LogInformation(
                    AppMessages.Logs.GameSetupDraftVersionConflict,
                    draftGame.Id,
                    update.ExpectedVersion,
                    board.Version
                );
                return new UpdateDraftSetupRepositoryResult(UpdateDraftSetupRepositoryStatus.StaleVersion);
            }

            await _dbContext.Entry(board)
                .Collection(existingBoard => existingBoard.Cells)
                .LoadAsync(cancellationToken);

            var rowCount = update.RowLabels.Count;
            var colCount = update.ColLabels.Count;
            var existingCells = board.Cells.ToList();
            var existingById = existingCells.ToDictionary(cell => cell.Id);
            var retainedUpdates = update.Cells
                .Select(cellUpdate => (
                    Update: cellUpdate,
                    CellId: Guid.TryParse(cellUpdate.CellId, out var parsedId) ? parsedId : (Guid?)null
                ))
                .Where(item => item.CellId.HasValue && existingById.ContainsKey(item.CellId.Value))
                .ToDictionary(item => item.CellId!.Value, item => item.Update);

            var cellsToRemove = existingCells
                .Where(cell => !retainedUpdates.ContainsKey(cell.Id))
                .ToList();

            var shouldVacatePositions = _dbContext.Database.IsRelational();
            await using var transaction = shouldVacatePositions
                ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
                : null;

            var retainedCells = existingCells
                .Where(cell => retainedUpdates.ContainsKey(cell.Id))
                .ToArray();

            if (shouldVacatePositions)
            {
                for (var index = 0; index < retainedCells.Length; index += 1)
                {
                    retainedCells[index].RowIndex = -1;
                    retainedCells[index].ColIndex = index;
                }
            }

            if (cellsToRemove.Count > 0)
            {
                _dbContext.BoardCells.RemoveRange(cellsToRemove);
            }

            if (shouldVacatePositions && (retainedCells.Length > 0 || cellsToRemove.Count > 0))
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (!shouldVacatePositions && existingCells.Count > 0)
            {
                _dbContext.BoardCells.RemoveRange(existingCells);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var cellUpdate in update.Cells)
            {
                if (shouldVacatePositions
                    && !string.IsNullOrWhiteSpace(cellUpdate.CellId)
                    && Guid.TryParse(cellUpdate.CellId, out var cellId)
                    && retainedUpdates.ContainsKey(cellId)
                    && existingById.TryGetValue(cellId, out var existingByIncomingId))
                {
                    existingByIncomingId.RowIndex = cellUpdate.Row;
                    existingByIncomingId.ColIndex = cellUpdate.Col;
                    existingByIncomingId.Title = cellUpdate.Title;
                    existingByIncomingId.Cost = cellUpdate.Cost;
                    continue;
                }

                var newCellId = Guid.TryParse(cellUpdate.CellId, out var incomingCellId)
                    && existingById.ContainsKey(incomingCellId)
                        ? incomingCellId
                        : Guid.NewGuid();

                _dbContext.BoardCells.Add(
                    new BoardCell
                    {
                        Id = newCellId,
                        BoardId = board.Id,
                        RowIndex = cellUpdate.Row,
                        ColIndex = cellUpdate.Col,
                        State = BoardCellState.Closed,
                        CellType = BoardCellPersistence.DefaultCellType,
                        Title = cellUpdate.Title,
                        Cost = cellUpdate.Cost,
                        Description = null
                    }
                );
            }

            var existingEnabledModifiers = await _dbContext.GameEnabledModifiers
                .Where(x => x.GameId == draftGame.Id)
                .ToListAsync(cancellationToken);
            var enabledIds = new HashSet<Guid>(update.EnabledModifierIds);
            var enabledModifiersToRemove = existingEnabledModifiers
                .Where(x => !enabledIds.Contains(x.ModifierId))
                .ToList();
            if (enabledModifiersToRemove.Count > 0)
            {
                _dbContext.GameEnabledModifiers.RemoveRange(enabledModifiersToRemove);
            }

            var existingIds = existingEnabledModifiers.Select(x => x.ModifierId).ToHashSet();
            var enabledAtUtc = DateTime.UtcNow;
            var enabledModifiersToAdd = enabledIds
                .Where(modifierId => !existingIds.Contains(modifierId))
                .Select(
                    modifierId =>
                        new GameEnabledModifier
                        {
                            GameId = draftGame.Id,
                            ModifierId = modifierId,
                            EnabledAtUtc = enabledAtUtc
                        }
                )
                .ToArray();
            if (enabledModifiersToAdd.Length > 0)
            {
                _dbContext.GameEnabledModifiers.AddRange(enabledModifiersToAdd);
            }

            var existingEnabledQuestions = await _dbContext.GameEnabledQuestions
                .Where(x => x.GameId == draftGame.Id)
                .ToListAsync(cancellationToken);
            var enabledQuestionIds = update.EnabledQuestionIds.ToHashSet();
            var enabledQuestionsToRemove = existingEnabledQuestions
                .Where(x => !enabledQuestionIds.Contains(x.QuestionId))
                .ToList();
            if (enabledQuestionsToRemove.Count > 0)
            {
                _dbContext.GameEnabledQuestions.RemoveRange(enabledQuestionsToRemove);
            }

            var existingQuestionIds = existingEnabledQuestions
                .Select(x => x.QuestionId)
                .ToHashSet();
            var enabledQuestionsToAdd = enabledQuestionIds
                .Where(questionId => !existingQuestionIds.Contains(questionId))
                .Select(
                    questionId =>
                        new GameEnabledQuestion
                        {
                            GameId = draftGame.Id,
                            QuestionId = questionId,
                            EnabledAtUtc = enabledAtUtc
                        }
                )
                .ToArray();
            if (enabledQuestionsToAdd.Length > 0)
            {
                _dbContext.GameEnabledQuestions.AddRange(enabledQuestionsToAdd);
            }

            draftGame.Title = update.Title;
            board.Rows = rowCount;
            board.Cols = colCount;
            board.RowLabels = update.RowLabels.ToArray();
            board.ColLabels = update.ColLabels.ToArray();
            board.Version += 1;

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation(
                AppMessages.Logs.GameSetupDraftSaved,
                draftGame.Id,
                board.Version
            );

            var snapshot = await BuildSnapshotAsync(
                new SelectedBoard(
                    board.Id,
                    draftGame.Id,
                    draftGame.Title,
                    draftGame.Description,
                    GameStatusValue.Draft,
                    board.Version,
                    board.Rows,
                    board.Cols,
                    board.RowLabels,
                    board.ColLabels
                ),
                cancellationToken
            );

            return new UpdateDraftSetupRepositoryResult(UpdateDraftSetupRepositoryStatus.Updated, snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.GameSetupDraftSaveFailed);
            throw;
        }
    }

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
            .Where(x => x.GameId == board.GameId)
            .OrderBy(x => x.ActivatedAtUtc)
            .Select(
                    x => new Application.Contracts.GameModifierActivation(
                        x.Id,
                        x.ModifierId,
                        x.ModifierDefinition.Name,
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
