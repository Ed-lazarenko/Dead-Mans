namespace backend.Domain.Persistence;

public static class GameCardRunStatusValue
{
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static string CheckSqlAllowedStatuses { get; } =
        $"\"Status\" IN ('{InProgress}','{Completed}','{Cancelled}')";

    public static string CheckSqlFinishedAtSemantics { get; } =
        "(("
        + "\"Status\" = '"
        + InProgress
        + "') AND "
        + "\"FinishedAtUtc\" IS NULL) OR (("
        + "\"Status\" IN ('"
        + Completed
        + "','"
        + Cancelled
        + "')) AND "
        + "\"FinishedAtUtc\" IS NOT NULL)";
}
