using backend.Application.Contracts;

namespace backend.Application.Features.GameSetup;

internal static class GameSetupDraftValidator
{
    public const int MaxTitleLength = 200;
    public const int MaxRowLabelLength = 100;
    public const int MaxColumnLabelLength = 100;
    public const int MaxCellTitleLength = 200;

    public static bool TryNormalizeTitle(string title, out string normalizedTitle)
    {
        normalizedTitle = title.Trim();
        return !string.IsNullOrWhiteSpace(normalizedTitle)
            && normalizedTitle.Length <= MaxTitleLength;
    }

    public static bool TryNormalizeRowLabels(
        IReadOnlyList<string> rowLabels,
        out string[] normalizedRowLabels
    )
    {
        normalizedRowLabels = rowLabels.Select(label => label.Trim()).ToArray();

        return normalizedRowLabels.Length is >= GameSetupBoardLimits.MinRows and <= GameSetupBoardLimits.MaxRows
            && normalizedRowLabels.All(
                label => !string.IsNullOrWhiteSpace(label) && label.Length <= MaxRowLabelLength
            );
    }

    public static bool TryNormalizeColumnLabels(
        IReadOnlyList<string> columnLabels,
        out string[] normalizedColumnLabels
    )
    {
        normalizedColumnLabels = columnLabels
            .Select(label => label.Trim())
            .ToArray();

        return normalizedColumnLabels.Length is >= GameSetupBoardLimits.MinCols and <= GameSetupBoardLimits.MaxCols
            && normalizedColumnLabels.All(
                label => !string.IsNullOrWhiteSpace(label) && label.Length <= MaxColumnLabelLength
            );
    }

    public static bool TryNormalizeCellUpdates(
        int rowCount,
        int colCount,
        IReadOnlyList<GameSetupCellUpdate> cells,
        out GameSetupCellUpdate[] normalizedCells
    )
    {
        var expectedCount = rowCount * colCount;
        if (cells.Count != expectedCount)
        {
            normalizedCells = Array.Empty<GameSetupCellUpdate>();
            return false;
        }

        var seenPositions = new HashSet<(int Row, int Col)>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<GameSetupCellUpdate>(cells.Count);

        foreach (var cell in cells)
        {
            if (cell.Row < 0
                || cell.Row >= rowCount
                || cell.Col < 0
                || cell.Col >= colCount
                || !seenPositions.Add((cell.Row, cell.Col)))
            {
                normalizedCells = Array.Empty<GameSetupCellUpdate>();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(cell.CellId))
            {
                if (!Guid.TryParse(cell.CellId, out _) || !seenIds.Add(cell.CellId))
                {
                    normalizedCells = Array.Empty<GameSetupCellUpdate>();
                    return false;
                }
            }

            if (cell.Cost < 0)
            {
                normalizedCells = Array.Empty<GameSetupCellUpdate>();
                return false;
            }

            var title = cell.Title?.Trim();
            if (title is { Length: > MaxCellTitleLength })
            {
                normalizedCells = Array.Empty<GameSetupCellUpdate>();
                return false;
            }

            normalized.Add(
                new GameSetupCellUpdate(
                    string.IsNullOrWhiteSpace(cell.CellId) ? null : cell.CellId,
                    cell.Row,
                    cell.Col,
                    string.IsNullOrWhiteSpace(title) ? null : title,
                    cell.Cost
                )
            );
        }

        normalizedCells = normalized.ToArray();
        return true;
    }

    public static bool TryNormalizeEnabledQuestionIds(
        IReadOnlyList<Guid> enabledQuestionIds,
        out Guid[] normalizedIds
    )
    {
        var uniqueIds = new HashSet<Guid>();
        var normalized = new List<Guid>(enabledQuestionIds.Count);
        foreach (var id in enabledQuestionIds)
        {
            if (id == Guid.Empty || !uniqueIds.Add(id))
            {
                normalizedIds = Array.Empty<Guid>();
                return false;
            }

            normalized.Add(id);
        }

        normalizedIds = normalized.ToArray();
        return true;
    }

    public static bool TryNormalizeEnabledModifierIds(
        IReadOnlyList<Guid> enabledModifierIds,
        out Guid[] normalizedIds
    )
    {
        var uniqueIds = new HashSet<Guid>();
        var normalized = new List<Guid>(enabledModifierIds.Count);
        foreach (var modifierId in enabledModifierIds)
        {
            if (modifierId == Guid.Empty || !uniqueIds.Add(modifierId))
            {
                normalizedIds = Array.Empty<Guid>();
                return false;
            }

            normalized.Add(modifierId);
        }

        normalizedIds = normalized.ToArray();
        return true;
    }
}
