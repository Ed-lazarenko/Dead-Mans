namespace backend.Application.Configuration;

public static class GameSetupDefaults
{
    public const int Rows = 5;
    public const int Cols = 5;

    private static readonly int[] RowCosts = [100, 125, 150, 175, 200];

    public static IReadOnlyList<int> DefaultRowCosts => RowCosts;

    public static string[] BuildRowLabels()
    {
        return Enumerable
            .Range(0, Rows)
            .Select(row => GetRowCost(row).ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
    }

    public static string[] BuildColumnLabels()
    {
        return Enumerable.Range(0, Cols).Select(GetColumnLabel).ToArray();
    }

    public static string GetColumnLabel(int columnIndex) =>
        (columnIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static int GetRowCost(int rowIndex)
    {
        if (rowIndex < RowCosts.Length)
        {
            return RowCosts[rowIndex];
        }

        var lastCost = RowCosts[^1];
        var extraRows = rowIndex - RowCosts.Length + 1;
        return lastCost + extraRows * 25;
    }
}
