namespace backend.Domain.Persistence;

public static class GameRoundTransitionActionValue
{
    public const string OpenOrdering = "open_ordering";
    public const string Prepare = "prepare";
    public const string Rebuild = "rebuild";
    public const string BeginGameplay = "begin_gameplay";
    public const string Review = "review";
    public const string ResumeGameplay = "resume_gameplay";
    public const string Finalize = "finalize";
    public const string TechnicalCancel = "technical_cancel";
}
