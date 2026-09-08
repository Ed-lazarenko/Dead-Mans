using backend.Messaging;

namespace backend.Application.Contracts;

public sealed record DraftGameLifecycleContext(
    Guid GameId,
    short MinPlayersPerTeam,
    short MaxPlayersPerTeam
);

public enum GameLifecycleErrorCode
{
    None,
    DraftNotFound,
    CurrentGameAlreadyExists,
    ActiveGameAlreadyExists,
    GameNotReady,
    ModifierVersionBindingMissing,
    GameNotActive,
    NoTeamSlots,
    InvalidTeamSizeLimits,
    NoConfirmedTeams,
    UnconfirmedTeams,
    PendingInvitations,
    PendingDisbandRequests,
    InvalidConfirmedTeamRoster,
    DraftDeleteNotAllowed,
    GameArchiveNotAllowed,
    GameNotFound,
    FinishRoundInProgress,
    FinishStaleVersion,
    FinishWarningsNotAcknowledged,
    FinishModifierStateInvalid,
    FinishInvalidRequest,
}

public sealed record GameLifecycleResult(bool Success, Guid? GameId, GameLifecycleErrorCode Error);

public static class GameFinishWarningCodes
{
    public const string UnplayedTeams = AppMessages.GameFinishConditions.UnplayedTeams;
    public const string NoCompletedRounds = AppMessages.GameFinishConditions.NoCompletedRounds;
}

public static class GameFinishBlockerCodes
{
    public const string RoundInProgress = AppMessages.GameFinishConditions.RoundInProgress;
    public const string ModifierStateInvalid = AppMessages.GameFinishConditions.ModifierStateInvalid;
}

public sealed record GameFinishIssue(string Code, int Count = 1);

public sealed record GameFinishTeamResult(
    Guid TeamId,
    string? TeamName,
    int TeamSlotIndex,
    IReadOnlyList<string> ParticipantNames,
    int RoundsPlayed,
    int? BestScore,
    int PenaltyTotal,
    int? FinalScore,
    int TotalScore,
    int TotalBonusDelta,
    int TotalKills,
    int TotalBounties,
    int? Placement,
    DateTime? LastFinishedAtUtc
);

public sealed record GameFinishSummary(
    Guid GameId,
    string GameTitle,
    string GameStatus,
    int BoardVersion,
    DateTime? FinishedAtUtc,
    Guid? FinishedByUserId,
    string? FinishedByDisplayName,
    string? PublicNote,
    int CalculationVersion,
    int CompletedRoundCount,
    int CancelledRoundCount,
    int TotalKills,
    int TotalBounties,
    int QuizTotalPoints,
    int PendingQuizQuestionCount,
    int SkippedQuizQuestionCount,
    IReadOnlyList<GameFinishTeamResult> Teams
);

public sealed record GameFinishPreview(
    GameFinishSummary Summary,
    bool CanFinish,
    IReadOnlyList<GameFinishIssue> Blockers,
    IReadOnlyList<GameFinishIssue> Warnings
);

public sealed record FinishGameInput(
    int ExpectedBoardVersion,
    Guid RequestId,
    IReadOnlySet<string> AcknowledgedWarningCodes,
    string? PublicNote
);

public sealed record GameFinishPreviewResult(
    GameLifecycleErrorCode Error,
    GameFinishPreview? Preview
)
{
    public bool Success => Error == GameLifecycleErrorCode.None && Preview is not null;
}

public sealed record FinishGameResult(
    GameLifecycleErrorCode Error,
    GameFinishSummary? Summary,
    bool AlreadyFinished = false
)
{
    public bool Success => Error == GameLifecycleErrorCode.None && Summary is not null;
}
