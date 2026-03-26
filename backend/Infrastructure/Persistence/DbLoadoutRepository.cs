using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbLoadoutRepository : ILoadoutRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string _storagePublicBaseUrl;
    private readonly ILogger<DbLoadoutRepository> _logger;

    public DbLoadoutRepository(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<DbLoadoutRepository> logger
    )
    {
        _dbContext = dbContext;
        _storagePublicBaseUrl = configuration["Storage:PublicBaseUrl"] ?? "http://localhost:9000";
        _logger = logger;
    }

    public async Task<LoadoutBoard> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoadLatestBoardAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while loading loadout board.");
            throw;
        }
    }

    public async Task<LoadoutBoard> ToggleCellStateAsync(
        string cellId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (!Guid.TryParse(cellId, out var parsedCellId))
            {
                throw new InvalidOperationException($"Loadout cell id '{cellId}' is not a valid GUID.");
            }

            var cell = await _dbContext.BoardCells.FirstOrDefaultAsync(x => x.Id == parsedCellId, cancellationToken);
            if (cell == null)
            {
                throw new InvalidOperationException($"Loadout cell '{cellId}' was not found.");
            }

            cell.State = cell.State == BoardCellState.Open
                ? BoardCellState.Closed
                : BoardCellState.Open;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadBoardByIdAsync(cell.BoardId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error while toggling loadout cell. CellId: {CellId}.", cellId);
            throw;
        }
    }

    private async Task<LoadoutBoard> LoadLatestBoardAsync(CancellationToken cancellationToken)
    {
        var boardId = await _dbContext.GameBoards
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (boardId == Guid.Empty)
        {
            throw new InvalidOperationException("No loadout board exists in the database.");
        }

        return await LoadBoardByIdAsync(boardId, cancellationToken);
    }

    private async Task<LoadoutBoard> LoadBoardByIdAsync(Guid boardId, CancellationToken cancellationToken)
    {
        var boardMetadata = await _dbContext.GameBoards
            .AsNoTracking()
            .Where(x => x.Id == boardId)
            .Select(x => new { x.Rows, x.Cols, x.RowLabels, x.ColLabels })
            .FirstOrDefaultAsync(cancellationToken);

        if (boardMetadata == null)
        {
            throw new InvalidOperationException($"Loadout board '{boardId}' metadata was not found.");
        }

        var rawCells = await _dbContext.BoardCells
            .AsNoTracking()
            .Where(x => x.BoardId == boardId)
            .OrderBy(x => x.RowIndex)
            .ThenBy(x => x.ColIndex)
            .Select(x => new RawBoardCell(
                x.Id,
                x.RowIndex,
                x.ColIndex,
                x.State,
                x.Title,
                x.Cost,
                x.MediaLinks
                    .OrderBy(link => link.SortOrder)
                    .Select(link => new RawMediaAsset(link.MediaAsset.Bucket, link.MediaAsset.ObjectKey))
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);

        if (rawCells.Count == 0)
        {
            throw new InvalidOperationException($"Loadout board '{boardId}' does not contain any cells.");
        }

        var cells = rawCells
            .Select(cell =>
            {
                var model = new LoadoutCell
                {
                    Id = cell.Id.ToString(),
                    Row = cell.Row,
                    Col = cell.Col,
                    Label = cell.Title ?? string.Empty,
                    Points = cell.Cost,
                    ImageUrl = cell.MediaAsset == null
                        ? null
                        : BuildPublicMediaUrl(cell.MediaAsset.Bucket, cell.MediaAsset.ObjectKey)
                };

                if (ParseCellState(cell.State) == LoadoutCellState.Open)
                {
                    model.ToggleOpen();
                }

                return model;
            })
            .ToArray();

        return new LoadoutBoard
        {
            Rows = boardMetadata.Rows,
            Cols = boardMetadata.Cols,
            RowLabels = boardMetadata.RowLabels,
            ColLabels = boardMetadata.ColLabels,
            Cells = cells
        };
    }

    private string BuildPublicMediaUrl(string bucket, string objectKey)
    {
        return $"{_storagePublicBaseUrl.TrimEnd('/')}/{bucket}/{objectKey}";
    }

    private static LoadoutCellState ParseCellState(BoardCellState state)
    {
        return state switch
        {
            BoardCellState.Open => LoadoutCellState.Open,
            _ => LoadoutCellState.Closed
        };
    }

    private sealed record RawBoardCell(
        Guid Id,
        int Row,
        int Col,
        BoardCellState State,
        string? Title,
        int Cost,
        RawMediaAsset? MediaAsset
    );

    private sealed record RawMediaAsset(string Bucket, string ObjectKey);
}
