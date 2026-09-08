using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.Models;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameBoardRepository
{
    public Task<bool> IsCurrentActiveGameCellAsync(
        Guid cellId,
        CancellationToken cancellationToken = default
    )
    {
        return _dbContext.BoardCells
            .AsNoTracking()
            .AnyAsync(
                cell =>
                    cell.Id == cellId
                    && cell.Board.Game.Status == GameStatusValue.Active
                    && !cell.Board.Game.IsDeleted,
                cancellationToken
            );
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

            var cell = await GameBoardCellOpenGuard.FindActiveGameCellAsync(
                _dbContext,
                cellId,
                cancellationToken
            );
            if (cell is null)
            {
                _logger.LogInformation(AppMessages.Logs.GameCellNotFoundForOpen, cellId);
                return null;
            }

            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM games WHERE id = {cell.GameId} FOR UPDATE",
                cancellationToken
            );
            cell = await GameBoardCellOpenGuard.FindActiveGameCellAsync(
                _dbContext,
                cellId,
                cancellationToken
            );
            if (cell is null)
            {
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
                .Select(x => new { x.GameId, x.Version, x.Game.ActiveTeamId })
                .FirstAsync(cancellationToken);

            if (stateChanged)
            {
                await CreateAwaitingModifiersRunAsync(
                    board.GameId,
                    board.ActiveTeamId,
                    cell,
                    cancellationToken
                );
            }

            await transaction.CommitAsync(cancellationToken);

            var mappedCell = await LoadOpenedCellAsync(
                new GameBoardCellProjection.RawCell(
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
        var activeCell = await GameBoardCellOpenGuard.FindActiveGameCellAsync(
            _dbContext,
            cellId,
            cancellationToken
        );
        if (activeCell is null)
        {
            _logger.LogInformation(AppMessages.Logs.GameCellNotFoundForOpen, cellId);
            return null;
        }

        var cell = await _dbContext.BoardCells.FirstOrDefaultAsync(
            x => x.Id == cellId,
            cancellationToken
        );
        if (cell is null)
        {
            _logger.LogInformation(AppMessages.Logs.GameCellNotFoundForOpen, cellId);
            return null;
        }

        var board = await _dbContext.GameBoards
            .Include(x => x.Game)
            .FirstAsync(x => x.Id == cell.BoardId, cancellationToken);
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
            await CreateAwaitingModifiersRunAsync(
                board.GameId,
                board.Game?.ActiveTeamId,
                activeCell,
                cancellationToken
            );
            stateChanged = true;
            _logger.LogInformation(AppMessages.Logs.GameCellOpened, cellId);
        }

        var mappedCell = await LoadOpenedCellAsync(
            new GameBoardCellProjection.RawCell(
                cell.Id,
                activeCell.Row,
                activeCell.Col,
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

    private async Task CreateAwaitingModifiersRunAsync(
        Guid gameId,
        Guid? activeTeamId,
        GameBoardCellOpenGuard.OpenCellTarget cell,
        CancellationToken cancellationToken
    )
    {
        if (!activeTeamId.HasValue)
        {
            return;
        }

        var hasActiveRound = await _dbContext.GameRounds.AnyAsync(
            round =>
                round.GameId == gameId
                && (round.Status == GameRoundStatusValue.AwaitingModifiers
                    || round.Status == GameRoundStatusValue.Preparing
                    || round.Status == GameRoundStatusValue.InProgress
                    || round.Status == GameRoundStatusValue.ReviewingResults),
            cancellationToken
        );
        if (hasActiveRound)
        {
            return;
        }

        var team = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(team => team.Id == activeTeamId.Value && team.GameId == gameId)
            .Select(team => new
            {
                team.Id,
                team.Status,
                team.IsPlayed,
                team.DisbandedAtUtc,
                SlotIndex = team.Slot != null ? team.Slot.SlotIndex : (int?)null
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null
            || team.Status != TeamStatusValue.Confirmed
            || team.IsPlayed
            || team.DisbandedAtUtc != null
            || !team.SlotIndex.HasValue)
        {
            return;
        }

        var participants = await _dbContext.GameTeamMembers
            .AsNoTracking()
            .Where(
                member =>
                    member.GameId == gameId
                    && member.TeamId == activeTeamId.Value
                    && member.LeftAtUtc == null
            )
            .OrderBy(member => member.JoinedAtUtc)
            .Select(member => new
            {
                member.UserId,
                DisplayName = member.User != null ? member.User.DisplayName : string.Empty
            })
            .ToArrayAsync(cancellationToken);
        if (participants.Length == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var cellMedia = await _dbContext.BoardCellMedia
            .AsNoTracking()
            .Where(link => link.CellId == cell.Id)
            .OrderBy(link => link.SortOrder)
            .Select(link => new
            {
                link.MediaAsset.Bucket,
                link.MediaAsset.ObjectKey,
                link.MediaAsset.MimeType,
                link.MediaAsset.SizeBytes,
                link.Role,
                link.SortOrder
            })
            .ToArrayAsync(cancellationToken);
        var round = new GameRound
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            BoardId = cell.BoardId,
            BoardCellId = cell.Id,
            TeamId = activeTeamId.Value,
            Status = GameRoundStatusValue.AwaitingModifiers,
            BaseScore = cell.Cost,
            TeamSlotIndexSnapshot = team.SlotIndex.Value,
            CellRowIndex = cell.Row,
            CellColIndex = cell.Col,
            CellTitleSnapshot = cell.Title,
            CellDescriptionSnapshot = cell.Description,
            CellCostSnapshot = cell.Cost,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        _dbContext.GameRounds.Add(round);
        _dbContext.GameRoundCellMedia.AddRange(
            cellMedia.Select(
                media =>
                    new GameRoundCellMedia
                    {
                        Id = Guid.NewGuid(),
                        RoundId = round.Id,
                        Bucket = media.Bucket,
                        ObjectKey = media.ObjectKey,
                        MimeType = media.MimeType,
                        SizeBytes = media.SizeBytes,
                        Role = media.Role,
                        SortOrder = media.SortOrder,
                        CreatedAtUtc = now
                    }
            )
        );
        _dbContext.GameRoundParticipants.AddRange(
            participants.Select(
                participant =>
                    new GameRoundParticipant
                    {
                        Id = Guid.NewGuid(),
                        RoundId = round.Id,
                        UserId = participant.UserId,
                        DisplayNameSnapshot = string.IsNullOrWhiteSpace(participant.DisplayName)
                            ? participant.UserId.ToString()
                            : participant.DisplayName,
                        CreatedAtUtc = now
                    }
            )
        );
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GameBoardCell> LoadOpenedCellAsync(
        GameBoardCellProjection.RawCell cell,
        CancellationToken cancellationToken
    )
    {
        var mediaByCellId = await GameBoardCellProjection.LoadMediaByCellIdAsync(
            _dbContext,
            _storagePublicBaseUrl,
            [cell.Id],
            cancellationToken
        );
        return GameBoardCellProjection.MapCell(
            cell with { State = BoardCellState.Open },
            mediaByCellId,
            revealClosedContent: false
        );
    }
}
