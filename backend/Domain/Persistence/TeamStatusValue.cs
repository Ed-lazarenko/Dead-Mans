namespace backend.Domain.Persistence;

public static class TeamStatusValue
{
    public const string Forming = "forming";
    public const string Confirmed = "confirmed";
    public const string Rejected = "rejected";
    public const string Disbanded = "disbanded";

    public static string CheckSqlAllowedStatuses { get; } =
        $"status IN ('{Forming}','{Confirmed}','{Rejected}','{Disbanded}')";

    public static bool OccupiesSlot(string status) =>
        status is Forming or Confirmed;

    public static string CheckSqlOccupyingStatuses { get; } =
        $"status IN ('{Forming}','{Confirmed}')";

    public static bool IsVisibleInRegistration(string status) =>
        status is Forming or Confirmed;
}
