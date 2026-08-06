namespace backend.Domain.Persistence;

public static class InvitedByKindValue
{
    public const string Admin = "admin";
    public const string Member = "member";

    public static string CheckSqlAllowed { get; } =
        $"invited_by_kind IN ('{Admin}','{Member}')";
}
