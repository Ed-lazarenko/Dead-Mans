namespace backend.Api.Contracts;

public sealed record UserGameModifierActivationHistoryItemDto(
    string ModifierId,
    DateTime ActivatedAtUtc
);

public sealed record UserGameQuestionAnswerHistoryItemDto(
    string RoundId,
    string QuestionId,
    string QuestionText,
    string CategoryName,
    DateTime AnsweredAtUtc,
    bool IsCorrect,
    int AwardedPoints,
    string? SubmittedAnswer,
    string? AnsweredByUserId
);

public sealed record UserGameQuizManualAwardHistoryItemDto(
    string AwardId,
    DateTime AwardedAtUtc,
    int AwardedPoints,
    string AwardedByUserId,
    string AwardedByDisplayName
);

public sealed record UserGameHistoryItemDto(
    string GameId,
    string GameTitle,
    string GameStatus,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    IReadOnlyList<UserGameModifierActivationHistoryItemDto> ModifierActivations,
    IReadOnlyList<UserGameQuestionAnswerHistoryItemDto> QuestionAnswers,
    IReadOnlyList<UserGameQuizManualAwardHistoryItemDto> ManualQuizAwards
);

public sealed record GameHistoryLeaderboardEntryDto(
    string UserId,
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

public sealed record GameHistoryGameSummaryDto(
    string GameId,
    string GameTitle,
    string GameStatus,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    int MainGameRunCount,
    int QuizRoundCount,
    int UniquePlayerCount
);

public sealed record GameHistoryPlayerSummaryDto(
    string UserId,
    string DisplayName,
    int Points,
    int EventCount,
    DateTime? LastActivityAtUtc
);

public sealed record GameHistoryModifierActivationItemDto(
    string ActivationId,
    string ModifierId,
    string ModifierName,
    string ActivatedByUserId,
    string ActivatedByDisplayName,
    DateTime ActivatedAtUtc
);

public sealed record GameHistoryRoundParticipantItemDto(
    string UserId,
    string DisplayName,
    DateTime CreatedAtUtc
);

public sealed record GameHistoryRoundModifierItemDto(
    string ModifierResultId,
    string ModifierId,
    string ModifierName,
    string ModifierDescription,
    string ModifierCategory,
    string ModifierMechanicType,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc
);

public sealed record GameHistoryRoundItemDto(
    string RoundId,
    string TeamId,
    int TeamSlotIndex,
    string Status,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    int BaseScore,
    int? FinalScore,
    int KillsCount,
    int BountyCount,
    string CellId,
    int CellRowIndex,
    int CellColIndex,
    string CellType,
    string? CellTitle,
    string? CellDescription,
    int CellCost,
    string? Notes,
    IReadOnlyList<GameBoardCellMediaDto> CellMedia,
    IReadOnlyList<GameHistoryRoundParticipantItemDto> Participants,
    IReadOnlyList<GameHistoryRoundModifierItemDto> Modifiers
);

public sealed record GameHistoryQuizRoundItemDto(
    string RoundId,
    string QuestionId,
    string QuestionCode,
    string QuestionText,
    string CategoryName,
    int Reward,
    string Status,
    DateTime AskedAtUtc,
    DateTime? AnsweredAtUtc,
    string? AnsweredByDisplayName,
    string? AnsweredByUserId,
    string? AnsweredForUserId,
    string? AnsweredForDisplayName,
    string? SubmittedAnswer,
    bool? IsCorrect,
    int? AwardedPoints
);

public sealed record GameHistoryQuizManualAwardItemDto(
    string AwardId,
    string AwardedToUserId,
    string AwardedToDisplayName,
    string AwardedByUserId,
    string AwardedByDisplayName,
    int AwardedPoints,
    DateTime AwardedAtUtc
);

public sealed record GameHistoryMainGameSectionDto(
    IReadOnlyList<GameHistoryPlayerSummaryDto> PlayerStats,
    IReadOnlyList<GameHistoryModifierActivationItemDto> ModifierActivations,
    IReadOnlyList<GameHistoryRoundItemDto> Rounds
);

public sealed record GameHistoryQuizSectionDto(
    IReadOnlyList<GameHistoryPlayerSummaryDto> PlayerStats,
    IReadOnlyList<GameHistoryQuizRoundItemDto> Rounds,
    IReadOnlyList<GameHistoryQuizManualAwardItemDto> ManualAwards
);

public sealed record GameHistoryGameDetailsDto(
    string GameId,
    string GameTitle,
    string GameStatus,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    GameHistoryMainGameSectionDto MainGame,
    GameHistoryQuizSectionDto Quiz
);
