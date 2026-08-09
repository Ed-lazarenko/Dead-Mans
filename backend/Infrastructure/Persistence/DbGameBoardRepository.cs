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
                        x.ModifierId,
                        x.ModifierDefinition.Name,
                        x.ActivatedByUserId.ToString(),
                        x.ActivatedByUser != null ? x.ActivatedByUser.DisplayName : x.ActivatedByUserId.ToString(),
                        x.ActivationCostSnapshot > 0
                            ? x.ActivationCostSnapshot
                            : x.ModifierDefinition.ActivationCost,
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

    public async Task<GameTeamQueueResult> GetCurrentTeamQueueAsync(
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
            return EmptyTeamQueueResult();
        }

        var rosters = await _dbContext.LoadConfirmedTeamRostersAsync(
            currentGameId.Value,
            cancellationToken
        );
        if (rosters.Count == 0)
        {
            return EmptyTeamQueueResult();
        }

        var teams = rosters
            .Select(roster =>
                new GameTeamQueueItem(
                    roster.TeamId,
                    roster.TeamName,
                    roster.TeamSlotIndex,
                    roster.IsPlayed,
                    roster.Participants
                        .Select(participant => new GameTeamQueueParticipant(
                            participant.UserId,
                            participant.DisplayName
                        ))
                        .ToArray()
                )
            )
            .ToArray();

        var playedTeams = teams.Count(x => x.IsPlayed);
        return new GameTeamQueueResult(
            new GameTeamQueueSummary(
                teams.Length,
                playedTeams,
                Math.Max(teams.Length - playedTeams, 0)
            ),
            teams
        );
    }

    private static GameTeamQueueResult EmptyTeamQueueResult()
    {
        return new GameTeamQueueResult(
            new GameTeamQueueSummary(0, 0, 0),
            Array.Empty<GameTeamQueueItem>()
        );
    }

    public async Task<SetActiveGameTeamOutcome> SetActiveTeamAsync(
        Guid? teamId,
        CancellationToken cancellationToken = default
    )
    {
        var activeGame = await QueryCurrentActiveGames().FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return SetActiveGameTeamOutcome.NoActiveGame;
        }

        if (activeGame.ActiveTeamId != teamId
            && await _dbContext.GameRounds.AnyAsync(
                round =>
                    round.GameId == activeGame.Id
                    && (round.Status == GameRoundStatusValue.AwaitingModifiers
                        || round.Status == GameRoundStatusValue.InProgress
                        || round.Status == GameRoundStatusValue.ReviewingResults),
                cancellationToken
            ))
        {
            return SetActiveGameTeamOutcome.RoundInProgress;
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
                        candidate.IsPlayed,
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

        if (team.IsPlayed)
        {
            return SetActiveGameTeamOutcome.TeamAlreadyPlayed;
        }

        if (team.ActiveMembersCount == 0)
        {
            return SetActiveGameTeamOutcome.TeamHasNoActiveMembers;
        }

        activeGame.ActiveTeamId = team.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return SetActiveGameTeamOutcome.Updated;
    }

    public async Task<SetGameTeamPlayedStateOutcome> SetGameTeamPlayedStateAsync(
        Guid teamId,
        bool isPlayed,
        CancellationToken cancellationToken = default
    )
    {
        var activeGame = await QueryCurrentActiveGames().FirstOrDefaultAsync(cancellationToken);
        if (activeGame is null)
        {
            return SetGameTeamPlayedStateOutcome.NoActiveGame;
        }

        if (await _dbContext.GameRounds.AnyAsync(
                round =>
                    round.GameId == activeGame.Id
                    && (round.Status == GameRoundStatusValue.AwaitingModifiers
                        || round.Status == GameRoundStatusValue.InProgress
                        || round.Status == GameRoundStatusValue.ReviewingResults),
                cancellationToken
            ))
        {
            return SetGameTeamPlayedStateOutcome.RoundInProgress;
        }

        var team = await _dbContext.GameTeams.FirstOrDefaultAsync(
            candidate => candidate.Id == teamId && candidate.GameId == activeGame.Id,
            cancellationToken
        );
        if (team is null)
        {
            return SetGameTeamPlayedStateOutcome.TeamNotFound;
        }

        if (team.Status != TeamStatusValue.Confirmed || team.DisbandedAtUtc != null)
        {
            return SetGameTeamPlayedStateOutcome.TeamNotConfirmed;
        }

        team.IsPlayed = isPlayed;
        team.UpdatedAtUtc = DateTime.UtcNow;

        if (isPlayed && activeGame.ActiveTeamId == team.Id)
        {
            activeGame.ActiveTeamId = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return SetGameTeamPlayedStateOutcome.Updated;
    }

    public async Task<bool> CurrentActiveGameHasActiveTeamAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGame = await QueryCurrentActiveGames()
            .AsNoTracking()
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
                    && !team.IsPlayed
                    && team.DisbandedAtUtc == null
            )
            .AnyAsync(team => team.Members.Any(member => member.LeftAtUtc == null), cancellationToken);
    }

    public async Task<bool> CurrentActiveGameHasActiveRoundAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await QueryCurrentActiveGames()
            .AsNoTracking()
            .Select(game => (Guid?)game.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return false;
        }

        return await _dbContext.GameRounds.AnyAsync(
            round =>
                round.GameId == activeGameId.Value
                && (round.Status == GameRoundStatusValue.AwaitingModifiers
                    || round.Status == GameRoundStatusValue.InProgress
                    || round.Status == GameRoundStatusValue.ReviewingResults),
            cancellationToken
        );
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

        var now = DateTime.UtcNow;
        var mediaByCellId = await GameBoardCellProjection.LoadMediaByCellIdAsync(
            _dbContext,
            _storagePublicBaseUrl,
            [cell.Id],
            cancellationToken
        );
        var cellMedia = mediaByCellId.TryGetValue(cell.Id, out var media) ? media : [];
        var round = new GameRound
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            BoardCellId = cell.Id,
            TeamId = activeTeamId.Value,
            Status = GameRoundStatusValue.AwaitingModifiers,
            StartedAtUtc = now,
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
                (media, index) =>
                    new GameRoundCellMedia
                    {
                        Id = Guid.NewGuid(),
                        RoundId = round.Id,
                        Url = media.Url,
                        SortOrder = index,
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

    private IQueryable<Game> QueryCurrentActiveGames()
    {
        return _dbContext.Games
            .Where(game => game.Status == GameStatusValue.Active && !game.IsDeleted)
            .OrderByDescending(game => game.StartedAtUtc ?? game.CreatedAtUtc);
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
