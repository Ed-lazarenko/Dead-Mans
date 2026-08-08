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
    int MainGameRoundsPlayed,
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
    int MainGameRoundCount,
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

public sealed record GameHistoryRoundParticipantItem(
    Guid UserId,
    string DisplayName,
    DateTime CreatedAtUtc
);

public sealed record GameHistoryRoundModifierItem(
    Guid ModifierResultId,
    Guid ModifierId,
    string ModifierName,
    string ModifierDescription,
    string ModifierCategory,
    string ModifierMechanicType,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAtUtc
);

public sealed record GameHistoryRoundItem(
    Guid RoundId,
    Guid TeamId,
    string? TeamName,
    int TeamSlotIndex,
    string Status,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    int BaseScore,
    int? FinalScore,
    int KillsCount,
    int BountyCount,
    Guid CellId,
    int CellRowIndex,
    int CellColIndex,
    string CellType,
    string? CellTitle,
    string? CellDescription,
    int CellCost,
    string? Notes,
    IReadOnlyList<GameBoardCellMedia> CellMedia,
    IReadOnlyList<GameHistoryRoundParticipantItem> Participants,
    IReadOnlyList<GameHistoryRoundModifierItem> Modifiers
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
    IReadOnlyList<GameHistoryRoundItem> Rounds
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
