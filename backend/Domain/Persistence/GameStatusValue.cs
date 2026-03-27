namespace backend.Domain.Persistence;

/// <summary>
/// Values stored in <c>games.Status</c> (check constraint <c>CK_games_status_allowed</c>).
/// SQL for EF Core <c>HasCheckConstraint</c> is centralized here so literals stay aligned with code
/// (the API only accepts a SQL string for checks).
/// </summary>
public static class GameStatusValue
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Finished = "finished";

    /// <summary>CHECK fragment: <c>"Status" IN (...)</c> (PostgreSQL-quoted column name).</summary>
    public static string CheckSqlAllowedStatuses { get; } =
        $"\"Status\" IN ('{Draft}','{Active}','{Finished}')";

    /// <summary>CHECK fragment for draft/active vs finished + <c>FinishedAtUtc</c>.</summary>
    public static string CheckSqlFinishedAtSemantics { get; } = BuildCheckSqlFinishedAtSemantics();

    private static string BuildCheckSqlFinishedAtSemantics()
    {
        string Q(string id) => "\"" + id + "\"";
        return
            "(("
            + Q("Status")
            + " IN ('"
            + Draft
            + "','"
            + Active
            + "')) AND "
            + Q("FinishedAtUtc")
            + " IS NULL) OR (("
            + Q("Status")
            + " = '"
            + Finished
            + "') AND "
            + Q("FinishedAtUtc")
            + " IS NOT NULL)";
    }
}
