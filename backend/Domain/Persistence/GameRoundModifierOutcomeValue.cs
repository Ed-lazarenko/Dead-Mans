namespace backend.Domain.Persistence;

public static class GameRoundModifierOutcomeValue
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Violated = "violated";
    public const string NotTriggered = "not_triggered";
    public const string Succeeded = "succeeded";
    public const string NotSucceeded = "not_succeeded";
    public const string Calculated = "calculated";

    public static string CheckSqlAllowedStatuses { get; } =
        $"outcome_status IN ('{Pending}','{Completed}','{Failed}','{Cancelled}',"
        + $"'{Violated}','{NotTriggered}','{Succeeded}','{NotSucceeded}','{Calculated}')";
}
