namespace backend.Domain.Persistence;
public static class GameStatusValue
{
    public const string Draft = "draft";
    public const string Ready = "ready";
    public const string Active = "active";
    public const string Finished = "finished";
    public static string CheckSqlAllowedStatuses { get; } =
        $"status IN ('{Draft}','{Ready}','{Active}','{Finished}')";
    public static string CheckSqlFinishedAtSemantics { get; } = BuildCheckSqlFinishedAtSemantics();

    public static string CheckSqlLifecycleTimestampSemantics { get; } =
        BuildCheckSqlLifecycleTimestampSemantics();

    public static string CheckSqlTeamSizeLimits { get; } =
        "min_players_per_team > 0 AND max_players_per_team >= min_players_per_team";

    private static string BuildCheckSqlFinishedAtSemantics()
    {
        string Q(string id) => id;
        return
            "(("
            + Q("status")
            + " IN ('"
            + Draft
            + "','"
            + Ready
            + "','"
            + Active
            + "')) AND "
            + Q("finished_at_utc")
            + " IS NULL) OR (("
            + Q("status")
            + " = '"
            + Finished
            + "') AND "
            + Q("finished_at_utc")
            + " IS NOT NULL)";
    }

    private static string BuildCheckSqlLifecycleTimestampSemantics()
    {
        string Q(string id) => id;
        return
            "(("
            + Q("status")
            + " = '"
            + Draft
            + "') AND "
            + Q("ready_at_utc")
            + " IS NULL AND "
            + Q("started_at_utc")
            + " IS NULL AND "
            + Q("finished_at_utc")
            + " IS NULL) OR (("
            + Q("status")
            + " = '"
            + Ready
            + "') AND "
            + Q("ready_at_utc")
            + " IS NOT NULL AND "
            + Q("started_at_utc")
            + " IS NULL AND "
            + Q("finished_at_utc")
            + " IS NULL) OR (("
            + Q("status")
            + " = '"
            + Active
            + "') AND "
            + Q("ready_at_utc")
            + " IS NOT NULL AND "
            + Q("started_at_utc")
            + " IS NOT NULL AND "
            + Q("finished_at_utc")
            + " IS NULL) OR (("
            + Q("status")
            + " = '"
            + Finished
            + "') AND "
            + Q("ready_at_utc")
            + " IS NOT NULL AND "
            + Q("started_at_utc")
            + " IS NOT NULL AND "
            + Q("finished_at_utc")
            + " IS NOT NULL)";
    }
}
