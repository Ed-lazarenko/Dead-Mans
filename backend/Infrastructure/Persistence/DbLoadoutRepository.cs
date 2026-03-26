using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed class DbLoadoutRepository : ILoadoutRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string _storagePublicBaseUrl;

    public DbLoadoutRepository(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _storagePublicBaseUrl = configuration["Storage:PublicBaseUrl"] ?? "http://localhost:9000";
    }

    public Task<LoadoutBoard> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        return LoadLatestBoardAsync(cancellationToken);
    }

    public async Task<LoadoutBoard> ToggleCellPlayedAsync(
        string cellId,
        CancellationToken cancellationToken = default
    )
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

        cell.State = cell.State.ToLowerInvariant() switch
        {
            "locked" => "locked",
            "played" => "available",
            _ => "played"
        };

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadBoardByIdAsync(cell.BoardId, cancellationToken);
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

        var rows = rawCells.Max(x => x.Row) + 1;
        var cols = rawCells.Max(x => x.Col) + 1;

        var rowLabels = Enumerable.Range(0, rows)
            .Select(row => rawCells
                .Where(x => x.Row == row)
                .OrderBy(x => x.Col)
                .Select(x => ParseTitle(x.Title).RowLabel)
                .FirstOrDefault() ?? $"{row + 1}")
            .ToArray();

        var colLabels = Enumerable.Range(0, cols)
            .Select(col => rawCells
                .Where(x => x.Col == col)
                .OrderBy(x => x.Row)
                .Select(x => ParseTitle(x.Title).ColLabel)
                .FirstOrDefault() ?? $"{col + 1}")
            .ToArray();

        var cells = rawCells
            .Select(cell =>
            {
                var titleParts = ParseTitle(cell.Title);
                var model = new LoadoutCell
                {
                    Id = cell.Id.ToString(),
                    Row = cell.Row,
                    Col = cell.Col,
                    Label = cell.Title,
                    Points = int.TryParse(titleParts.RowLabel, out var points) ? points : 0,
                    ImageUrl = cell.MediaAsset == null
                        ? null
                        : BuildPublicMediaUrl(cell.MediaAsset.Bucket, cell.MediaAsset.ObjectKey)
                };

                switch (ParseCellState(cell.State))
                {
                    case LoadoutCellState.Played:
                        model.TogglePlayed();
                        break;
                    case LoadoutCellState.Locked:
                        model.Lock();
                        break;
                }

                return model;
            })
            .ToArray();

        return new LoadoutBoard
        {
            Rows = rows,
            Cols = cols,
            RowLabels = rowLabels,
            ColLabels = colLabels,
            Cells = cells
        };
    }

    private string BuildPublicMediaUrl(string bucket, string objectKey)
    {
        return $"{_storagePublicBaseUrl.TrimEnd('/')}/{bucket}/{objectKey}";
    }

    private static LoadoutCellState ParseCellState(string state)
    {
        return state.ToLowerInvariant() switch
        {
            "played" => LoadoutCellState.Played,
            "locked" => LoadoutCellState.Locked,
            _ => LoadoutCellState.Available
        };
    }

    private static (string RowLabel, string ColLabel) ParseTitle(string title)
    {
        var parts = title.Split("•", 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }

        return (title, title);
    }

    private sealed record RawBoardCell(
        Guid Id,
        int Row,
        int Col,
        string State,
        string Title,
        RawMediaAsset? MediaAsset
    );

    private sealed record RawMediaAsset(string Bucket, string ObjectKey);
}
