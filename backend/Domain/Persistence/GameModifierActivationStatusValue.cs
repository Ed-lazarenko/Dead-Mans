namespace backend.Domain.Persistence;

public static class GameModifierActivationStatusValue
{
    public const string Active = "active";
    public const string Consumed = "consumed";
    public const string Cancelled = "cancelled";

    public const string CheckSqlAllowedStatuses =
        $"status IN ('{Active}','{Consumed}','{Cancelled}')";
}
