namespace backend.Domain.Persistence;

public static class GameCardRunStatusValue
{
    public const string AwaitingModifiers = "awaiting_modifiers";
    public const string InProgress = "in_progress";
    public const string ReviewingResults = "reviewing_results";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static string CheckSqlAllowedStatuses { get; } =
        $"status IN ('{AwaitingModifiers}','{InProgress}','{ReviewingResults}','{Completed}','{Cancelled}')";

    public static string CheckSqlFinishedAtSemantics { get; } =
        $"((status IN ('{AwaitingModifiers}','{InProgress}','{ReviewingResults}')) AND finished_at_utc IS NULL) "
        + $"OR ((status IN ('{Completed}','{Cancelled}')) AND finished_at_utc IS NOT NULL)";
}
