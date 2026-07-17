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
                .Where(cell => cell.State == BoardCellState.Open)
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
            var enabledModifierIds = await _dbContext.GameModifierSelections
                .AsNoTracking()
                .Where(x => x.GameId == selectedBoard.GameId)
                .OrderBy(x => x.ModifierId)
                .Select(x => x.ModifierId)
                .ToArrayAsync(cancellationToken);
            var activeModifiers = await _dbContext.GameActiveModifiers
                .AsNoTracking()
                .Where(x => x.GameId == selectedBoard.GameId)
                .OrderBy(x => x.ActivatedAtUtc)
                .Select(
                    x => new GameModifierActivation(
                        x.ModifierId,
                        x.ActivatedByUserId.ToString(),
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

    public async Task<IReadOnlyList<GameTeamQueueItem>> GetCurrentTeamQueueAsync(
        CancellationToken cancellationToken = default
    )
    {
        var currentGameId = await _dbContext.Games
            .AsNoTracking()
            .Where(
                x =>
                    !x.IsDeleted
                    && (x.Status == GameStatusValue.Active || x.Status == GameStatusValue.Ready)
            )
            .OrderByDescending(x => x.Status == GameStatusValue.Active)
            .ThenByDescending(x => x.StartedAtUtc ?? x.ReadyAtUtc ?? x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!currentGameId.HasValue)
        {
            return Array.Empty<GameTeamQueueItem>();
        }

        var rosters = await _dbContext.LoadConfirmedTeamRostersAsync(
            currentGameId.Value,
            cancellationToken
        );
        if (rosters.Count == 0)
        {
            return Array.Empty<GameTeamQueueItem>();
        }

        return rosters
            .Select(roster =>
                new GameTeamQueueItem(
                    roster.TeamId,
                    roster.TeamSlotIndex,
                    roster.Participants
                        .Select(participant => new GameTeamQueueParticipant(
                            participant.UserId,
                            participant.DisplayName
                        ))
                        .ToArray()
                )
            )
            .ToArray();
    }

    public async Task<SetActiveGameTeamOutcome> SetCurrentActiveTeamAsync(
        Guid? teamId,
        CancellationToken cancellationToken = default
    )
    {
        var activeGame = await _dbContext.Games
            .FirstOrDefaultAsync(
                game => game.Status == GameStatusValue.Active && !game.IsDeleted,
                cancellationToken
            );
        if (activeGame is null)
        {
            return SetActiveGameTeamOutcome.NoActiveGame;
        }

        if (!teamId.HasValue)
        {
            activeGame.ActiveTeamId = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return SetActiveGameTeamOutcome.Updated;
        }

        var team = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(candidate => candidate.Id == teamId.Value && candidate.GameId == activeGame.Id)
            .Select(
                candidate =>
                    new
                    {
                        candidate.Id,
                        candidate.Status,
                        candidate.DisbandedAtUtc,
                        ActiveMembersCount = candidate.Members.Count(member => member.LeftAtUtc == null),
                    }
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
        {
            return SetActiveGameTeamOutcome.TeamNotFound;
        }

        if (team.Status != TeamStatusValue.Confirmed || team.DisbandedAtUtc != null)
        {
            return SetActiveGameTeamOutcome.TeamNotConfirmed;
        }

        if (team.ActiveMembersCount == 0)
        {
            return SetActiveGameTeamOutcome.TeamHasNoActiveMembers;
        }

        activeGame.ActiveTeamId = team.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return SetActiveGameTeamOutcome.Updated;
    }

    public async Task<bool> CurrentActiveGameHasSelectedTeamAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGame = await _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Status == GameStatusValue.Active && !game.IsDeleted)
            .Select(game => new { game.Id, game.ActiveTeamId })
            .FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return true;
        }

        if (!activeGame.ActiveTeamId.HasValue)
        {
            return false;
        }

        return await _dbContext.GameTeams
            .AsNoTracking()
            .Where(
                team =>
                    team.Id == activeGame.ActiveTeamId.Value
                    && team.GameId == activeGame.Id
                    && team.Status == TeamStatusValue.Confirmed
                    && team.DisbandedAtUtc == null
            )
            .AnyAsync(team => team.Members.Any(member => member.LeftAtUtc == null), cancellationToken);
    }

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
