namespace backend.Domain.Persistence;

public static class ParticipationInvitationStatusValue
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Declined = "declined";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";

    public static string CheckSqlAllowedStatuses { get; } =
        $"status IN ('{Pending}','{Accepted}','{Declined}','{Cancelled}','{Expired}')";

    public static bool BlocksSlot(string status) => status == Pending;
}
