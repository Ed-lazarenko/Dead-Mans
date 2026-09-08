using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameSetupRepository
{
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
                    retainedCells[index].RowIndex = board.Rows + index;
                    retainedCells[index].ColIndex = 0;
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
            var enabledAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
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
            var questionSnapshots = await _dbContext.QuestionDefinitions
                .AsNoTracking()
                .Where(question => enabledQuestionIds.Contains(question.Id))
                .Select(question => new QuestionSnapshotData(
                    question.Id,
                    question.Revision,
                    question.ExternalCode,
                    question.CategoryDefinition != null
                        ? question.CategoryDefinition.Name
                        : string.Empty,
                    question.Text,
                    question.AcceptedAnswers
                        .OrderByDescending(answer => answer.IsPrimary)
                        .ThenBy(answer => answer.SortOrder)
                        .Select(answer => answer.AnswerText)
                        .ToArray(),
                    question.AcceptedAnswers
                        .OrderByDescending(answer => answer.IsPrimary)
                        .ThenBy(answer => answer.SortOrder)
                        .Select(answer => answer.NormalizedAnswer)
                        .ToArray(),
                    question.Reward,
                    question.Priority
                ))
                .ToDictionaryAsync(question => question.Id, cancellationToken);

            foreach (var enabledQuestion in existingEnabledQuestions.Where(
                         item => enabledQuestionIds.Contains(item.QuestionId)))
            {
                ApplyQuestionSnapshot(
                    enabledQuestion,
                    questionSnapshots[enabledQuestion.QuestionId],
                    enabledAtUtc
                );
            }

            var enabledQuestionsToAdd = enabledQuestionIds
                .Where(questionId => !existingQuestionIds.Contains(questionId))
                .Select(
                    questionId =>
                    {
                        var enabledQuestion = new GameEnabledQuestion
                        {
                            GameId = draftGame.Id,
                            QuestionId = questionId,
                            EnabledAtUtc = enabledAtUtc
                        };
                        ApplyQuestionSnapshot(
                            enabledQuestion,
                            questionSnapshots[questionId],
                            enabledAtUtc
                        );
                        return enabledQuestion;
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

    private static void ApplyQuestionSnapshot(
        GameEnabledQuestion enabledQuestion,
        QuestionSnapshotData question,
        DateTime snapshotAtUtc
    )
    {
        enabledQuestion.QuestionRevisionSnapshot = question.Revision;
        enabledQuestion.QuestionCodeSnapshot = question.ExternalCode;
        enabledQuestion.CategoryNameSnapshot = question.CategoryName;
        enabledQuestion.QuestionTextSnapshot = question.Text;
        enabledQuestion.AcceptedAnswersSnapshot = question.AcceptedAnswers;
        enabledQuestion.NormalizedAnswersSnapshot = question.NormalizedAnswers;
        enabledQuestion.RewardSnapshot = question.Reward;
        enabledQuestion.PrioritySnapshot = question.Priority;
        enabledQuestion.SnapshotAtUtc = snapshotAtUtc;
    }
}
