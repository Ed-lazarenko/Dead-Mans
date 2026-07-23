namespace backend.Application.Contracts;

public sealed record UserGameModifierActivationHistoryItem(
    Guid ModifierId,
    DateTime ActivatedAtUtc
);

public sealed record UserGameQuestionAnswerHistoryItem(
    Guid RoundId,
    Guid QuestionId,
    string QuestionText,
    string CategoryName,
    DateTime AnsweredAtUtc,
    bool IsCorrect,
    int AwardedPoints,
    string? SubmittedAnswer,
    Guid? AnsweredByUserId
);

public sealed record UserGameQuizManualAwardHistoryItem(
    Guid AwardId,
    DateTime AwardedAtUtc,
    int AwardedPoints,
    Guid AwardedByUserId,
    string AwardedByDisplayName
);

public sealed record UserGameHistoryItem(
    Guid GameId,
    string GameTitle,
    string GameStatus,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    IReadOnlyList<UserGameModifierActivationHistoryItem> ModifierActivations,
    IReadOnlyList<UserGameQuestionAnswerHistoryItem> QuestionAnswers,
    IReadOnlyList<UserGameQuizManualAwardHistoryItem> ManualQuizAwards
);

public sealed record GameHistoryLeaderboardEntry(
    Guid UserId,
    string DisplayName,
    int MainGamePoints,
    int QuizPoints,
    int TotalPoints,
    int GamesPlayed,
    int MainGameRunsPlayed,
    int QuizRoundsAnswered,
    int CorrectQuizAnswers,
    int ModifiersActivated,
    DateTime? LastActivityAtUtc
);

public sealed record GameHistoryGameSummary(
    Guid GameId,
    string GameTitle,
    string GameStatus,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    int MainGameRunCount,
    int QuizRoundCount,
    int UniquePlayerCount
);

public sealed record GameHistoryPlayerSummary(
    Guid UserId,
    string DisplayName,
    int Points,
    int EventCount,
    DateTime? LastActivityAtUtc
);

public sealed record GameHistoryModifierActivationItem(
    Guid ActivationId,
    Guid ModifierId,
    string ModifierName,
    Guid ActivatedByUserId,
    string ActivatedByDisplayName,
    DateTime ActivatedAtUtc
);

public sealed record GameHistoryCardRunParticipantItem(
    Guid UserId,
    string DisplayName,
    DateTime CreatedAtUtc
);

public sealed record GameHistoryCardRunModifierItem(
    Guid ModifierResultId,
    Guid ModifierId,
    string ModifierName,
    string ModifierCategory,
    string ModifierMechanicType,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAtUtc
);

public sealed record GameHistoryCardRunItem(
    Guid CardRunId,
    Guid TeamId,
    int TeamSlotIndex,
    string Status,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    int BaseScore,
    int? FinalScore,
    int KillsCount,
    int BountyCount,
    int CellRowIndex,
    int CellColIndex,
    string? CellTitle,
    int CellCost,
    string? Notes,
    IReadOnlyList<GameHistoryCardRunParticipantItem> Participants,
    IReadOnlyList<GameHistoryCardRunModifierItem> Modifiers
);

public sealed record GameHistoryQuizRoundItem(
    Guid RoundId,
    Guid QuestionId,
    string QuestionCode,
    string QuestionText,
    string CategoryName,
    int Reward,
    string Status,
    DateTime AskedAtUtc,
    DateTime? AnsweredAtUtc,
    string? AnsweredByDisplayName,
    Guid? AnsweredByUserId,
    Guid? AnsweredForUserId,
    string? AnsweredForDisplayName,
    string? SubmittedAnswer,
    bool? IsCorrect,
    int? AwardedPoints
);

public sealed record GameHistoryQuizManualAwardItem(
    Guid AwardId,
    Guid AwardedToUserId,
    string AwardedToDisplayName,
    Guid AwardedByUserId,
    string AwardedByDisplayName,
    int AwardedPoints,
    DateTime AwardedAtUtc
);

public sealed record GameHistoryMainGameSection(
    IReadOnlyList<GameHistoryPlayerSummary> PlayerStats,
    IReadOnlyList<GameHistoryModifierActivationItem> ModifierActivations,
    IReadOnlyList<GameHistoryCardRunItem> CardRuns
);

public sealed record GameHistoryQuizSection(
    IReadOnlyList<GameHistoryPlayerSummary> PlayerStats,
    IReadOnlyList<GameHistoryQuizRoundItem> Rounds,
    IReadOnlyList<GameHistoryQuizManualAwardItem> ManualAwards
);

public sealed record GameHistoryGameDetails(
    Guid GameId,
    string GameTitle,
    string GameStatus,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    GameHistoryMainGameSection MainGame,
    GameHistoryQuizSection Quiz
);
