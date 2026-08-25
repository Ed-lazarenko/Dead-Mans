namespace backend.Domain.Persistence;

public static class GameQuizManualAdjustmentOperationValue
{
    public const string Award = "award";
    public const string Deduct = "deduct";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [Award, Deduct],
        StringComparer.Ordinal
    );
}
