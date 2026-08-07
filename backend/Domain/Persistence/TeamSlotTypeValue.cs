namespace backend.Domain.Persistence;

public static class TeamSlotTypeValue
{
    public const string Public = "public";
    public const string Reserved = "reserved";

    public static string CheckSqlAllowed { get; } =
        $"slot_type IN ('{Public}','{Reserved}')";
}
