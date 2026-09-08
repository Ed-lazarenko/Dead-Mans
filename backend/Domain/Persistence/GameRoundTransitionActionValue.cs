namespace backend.Domain.Persistence;

public static class GameRoundTransitionActionValue
{
    public const string Prepare = "prepare";
    public const string Rebuild = "rebuild";
    public const string BeginGameplay = "begin_gameplay";
    public const string Review = "review";
    public const string ResumeGameplay = "resume_gameplay";
    public const string Finalize = "finalize";
    public const string TechnicalCancel = "technical_cancel";

    public static string CheckSqlAllowed { get; } =
        $"action_code IN ('{Prepare}','{Rebuild}','{BeginGameplay}',"
        + $"'{Review}','{ResumeGameplay}','{Finalize}','{TechnicalCancel}')";

    public static string CheckSqlTransitionSemantics { get; } =
        $"(action_code = '{Prepare}' AND from_status = 'awaiting_modifiers' AND to_status = 'preparing') OR "
        + $"(action_code = '{Rebuild}' AND from_status = 'preparing' AND to_status = 'awaiting_modifiers') OR "
        + $"(action_code = '{BeginGameplay}' AND from_status IN ('awaiting_modifiers','preparing') AND to_status = 'in_progress') OR "
        + $"(action_code = '{Review}' AND from_status = 'in_progress' AND to_status = 'reviewing_results') OR "
        + $"(action_code = '{ResumeGameplay}' AND from_status = 'reviewing_results' AND to_status = 'in_progress') OR "
        + $"(action_code = '{Finalize}' AND from_status = 'reviewing_results' AND to_status = 'completed') OR "
        + $"(action_code = '{TechnicalCancel}' AND from_status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results') AND to_status = 'cancelled')";
}
