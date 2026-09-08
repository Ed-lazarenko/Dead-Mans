namespace backend.Domain.Persistence;

public static class GameQuizPointEntryTypeValue
{
    public const string QuizReward = "quiz_reward";
    public const string ManualAdjustment = "manual_adjustment";
    public const string ModifierPurchase = "modifier_purchase";
    public const string ModifierRefund = "modifier_refund";

    public static string CheckSqlAllowed { get; } =
        $"entry_type IN ('{QuizReward}','{ManualAdjustment}','{ModifierPurchase}','{ModifierRefund}')";
}
