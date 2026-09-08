using backend.Application.Configuration;
using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameSetupRepository
{
    public async Task<GameBoardSnapshot?> CreateDraftSetupAsync(
        string title,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
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
}
