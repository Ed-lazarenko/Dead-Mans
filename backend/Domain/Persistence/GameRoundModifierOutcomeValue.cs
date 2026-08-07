namespace backend.Domain.Persistence;

public static class GameRoundModifierOutcomeValue
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";

    public static string CheckSqlAllowedStatuses { get; } =
        $"outcome_status IN ('{Pending}','{Completed}','{Failed}','{Cancelled}')";
}
