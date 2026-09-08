namespace backend.Domain.Persistence;

public static class GameQuizDeliveryKindValue
{
    public const string Manual = "manual";
    public const string Twitch = "twitch";

    public static string CheckSqlAllowed { get; } =
        $"delivery_kind IN ('{Manual}','{Twitch}')";
}
