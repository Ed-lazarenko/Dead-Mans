namespace backend.Domain.Persistence;

public static class GameQuizRoundStatusValue
{
    public const string Asked = "asked";
    public const string AnsweredCorrect = "answered_correct";
    public const string AnsweredWrong = "answered_wrong";
    public const string Timeout = "timeout";
    public const string Skipped = "skipped";

    public static string CheckSqlAllowedStatuses { get; } =
        $"status IN ('{Asked}','{AnsweredCorrect}','{AnsweredWrong}','{Timeout}','{Skipped}')";
}
