using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbGameBoardRepository : IGameBoardRepository
{
    private const string ActiveStatus = "active";
    private const string FinishedStatus = "finished";

    private readonly ApplicationDbContext _dbContext;
    private readonly string _storagePublicBaseUrl;
    private readonly ILogger<DbGameBoardRepository> _logger;

    public DbGameBoardRepository(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<DbGameBoardRepository> logger
    )
    {
        _dbContext = dbContext;
        _storagePublicBaseUrl = configuration["Storage:PublicBaseUrl"] ?? "http://localhost:9000";
        _logger = logger;
    }

    public async Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var selectedGame = await _dbContext.Games
                .AsNoTracking()
                .Where(game => game.Status == ActiveStatus)
                .OrderByDescending(game => game.StartedAtUtc ?? game.CreatedAtUtc)
                .Select(game => new { game.Id, game.Title, game.Description, game.Status })
                .FirstOrDefaultAsync(cancellationToken);

            if (selectedGame is null)
            {
                selectedGame = await _dbContext.Games
                    .AsNoTracking()
                    .Where(game => game.Status == FinishedStatus)
                    .OrderByDescending(game => game.FinishedAtUtc ?? game.CreatedAtUtc)
                    .Select(game => new { game.Id, game.Title, game.Description, game.Status })
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (selectedGame is null)
            {
                _logger.LogDebug("No active or finished game row found.");
                return null;
            }

            var board = await _dbContext.GameBoards
                .AsNoTracking()
                .Where(gameBoard => gameBoard.GameId == selectedGame.Id)
                .Select(gameBoard => new { gameBoard.Id, gameBoard.Rows, gameBoard.Cols, gameBoard.RowLabels, gameBoard.ColLabels })
                .FirstOrDefaultAsync(cancellationToken);

            if (board is null)
            {
                _logger.LogWarning(
                    "Game {GameId} has no board row; cannot build snapshot.",
                    selectedGame.Id
                );
                return null;
            }

            var cells = await _dbContext.BoardCells
            .AsNoTracking()
            .Where(cell => cell.BoardId == board.Id)
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

        var mediaByCellId = new Dictionary<Guid, List<GameBoardCellMedia>>();
        if (openCellIds.Length > 0)
        {
            var mediaRows = await _dbContext.BoardCellMedia
                .AsNoTracking()
                .Where(link => openCellIds.Contains(link.CellId))
                .OrderBy(link => link.SortOrder)
                .Select(link => new { link.CellId, link.MediaAsset.Bucket, link.MediaAsset.ObjectKey })
                .ToListAsync(cancellationToken);

            mediaByCellId = mediaRows
                .GroupBy(row => row.CellId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(item => new GameBoardCellMedia(
                            $"{_storagePublicBaseUrl.TrimEnd('/')}/{item.Bucket}/{item.ObjectKey}"
                        ))
                        .ToList()
                );
        }

        var resultCells = cells
            .Select(cell =>
            {
                var state = cell.State == BoardCellState.Open
                    ? LoadoutCellState.Open
                    : LoadoutCellState.Closed;

                var media = state == LoadoutCellState.Open
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
                "Resolved game board snapshot. GameId: {GameId}, Status: {Status}, CellCount: {CellCount}.",
                selectedGame.Id,
                selectedGame.Status,
                resultCells.Length
            );

            return new GameBoardSnapshot(
                selectedGame.Id.ToString(),
                selectedGame.Title,
                selectedGame.Description,
                selectedGame.Status,
                board.Rows,
                board.Cols,
                board.RowLabels,
                board.ColLabels,
                resultCells
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while loading current game board.");
            throw;
        }
    }

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
}
