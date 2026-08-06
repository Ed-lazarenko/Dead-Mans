namespace backend.Domain.Persistence;
public static class BoardCellPersistence
{
    public const string DefaultCellType = "tile";

    public const string StateOpen = "open";
    public const string StateClosed = "closed";
    public static string CheckSqlAllowedStates { get; } =
        $"state IN ('{StateOpen}','{StateClosed}')";
}
