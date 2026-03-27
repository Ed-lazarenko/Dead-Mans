namespace backend.Domain.Persistence;

/// <summary>
/// String values persisted for board cell fields (see EF conversions and seed migrations).
/// </summary>
public static class BoardCellPersistence
{
    public const string CellTypeLoadout = "loadout";

    public const string StateOpen = "open";
    public const string StateClosed = "closed";

    /// <summary>CHECK fragment: <c>"State" IN (...)</c> for <c>CK_board_cells_state_allowed</c>.</summary>
    public static string CheckSqlAllowedStates { get; } =
        $"\"State\" IN ('{StateOpen}','{StateClosed}')";
}
