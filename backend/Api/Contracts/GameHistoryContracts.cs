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
    int MainGameRoundsPlayed,
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
    int MainGameRoundCount,
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
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    string ActivationId,
    int DefinitionRevision,
    string? ResolutionKind,
    string? ViolationComment
);

public sealed record GameHistoryRoundItemDto(
    string RoundId,
    string TeamId,
    string? TeamName,
    int TeamSlotIndex,
    string Status,
    int RoundVersion,
    DateTime StartedAtUtc,
    DateTime? PreparedAtUtc,
    DateTime? GameplayStartedAtUtc,
    DateTime? ReviewedAtUtc,
    DateTime? FinishedAtUtc,
    int BaseScore,
    int? FinalScore,
    bool EmptyCardPenaltyApplied,
    GameRoundScoreDetailsDto ScoreDetails,
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
    string? TechnicalCancellationReasonCode,
    string? PublicCancellationSummary,
    string? TechnicalCancellationStage,
    bool PurchasesRefunded,
    IReadOnlyList<GameBoardCellMediaDto> CellMedia,
    IReadOnlyList<GameHistoryRoundParticipantItemDto> Participants,
    IReadOnlyList<GameHistoryRoundModifierItemDto> Modifiers
);

public sealed record GameHistoryTeamLeaderboardEntryDto(
    string TeamId,
    string? TeamName,
    int TeamSlotIndex,
    int RoundsPlayed,
    int BestScore,
    int PenaltyTotal,
    int FinalScore,
    GameHistoryRoundItemDto BestRound,
    GameHistoryRoundItemDto LatestRound,
    IReadOnlyList<GameHistoryRoundItemDto> Rounds,
    int TotalScore,
    int AverageScore,
    int TotalBonusDelta,
    int TotalKills,
    int TotalBounties,
    IReadOnlyList<string> ParticipantNames,
    DateTime LastFinishedAtUtc
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
    IReadOnlyList<GameHistoryTeamLeaderboardEntryDto> TeamStats,
    IReadOnlyList<GameHistoryModifierActivationItemDto> ModifierActivations,
    IReadOnlyList<GameHistoryRoundItemDto> Rounds
);

public sealed record GameHistoryQuizSectionDto(
    int TotalPoints,
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
