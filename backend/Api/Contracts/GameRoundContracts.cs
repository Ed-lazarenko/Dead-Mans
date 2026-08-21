namespace backend.Api.Contracts;

public sealed record GameRoundParticipantDto(string UserId, string DisplayName);

public sealed record GameRoundModifierRuntimeBehaviorDto(
    string Phase,
    string Performer,
    bool RequiresHostMonitoring,
    string Rule,
    string StackingPolicy,
    int? DurationSecondsPerActivation
);

public sealed record GameRoundTeamOptionDto(
    string TeamId,
    string? TeamName,
    int TeamSlotIndex,
    IReadOnlyList<GameRoundParticipantDto> Participants
);

public sealed record GameRoundModifierResultDto(
    string ModifierResultId,
    string ModifierId,
    string ModifierName,
    string ModifierCategory,
    string ModifierDescription,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    string ActivationId,
    int DefinitionRevision,
    string? ResolutionGroupId,
    string? ResolutionKind,
    string? ViolationComment,
    GameRoundModifierRuntimeBehaviorDto? RuntimeBehavior
);

public sealed record GameRoundScoreDetailsDto(
    int ScoreUnit,
    int KillsScore,
    int BountyScore,
    int ModifierKillDelta,
    int ModifierKillScore,
    int ModifierScoreDelta,
    bool EmptyCardPenaltyApplied,
    int EmptyCardPenaltyScore,
    int PenaltyTotal,
    int BonusDelta,
    int TotalKillCount,
    int FinalScore
);

public sealed record GameRoundDetailsDto(
    string RoundId,
    string GameId,
    string CellId,
    string? CellTitle,
    string? CellDescription,
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
    string? Notes,
    string? TechnicalCancellationReasonCode,
    string? PublicCancellationSummary,
    DateTime ServerNowUtc,
    IReadOnlyList<GameRoundParticipantDto> Participants,
    IReadOnlyList<GameRoundModifierResultDto> ModifierResults
);

public sealed record StartGameRoundRequestDto(string CellId, string TeamId);

public sealed record GameRoundVersionCommandRequestDto(int ExpectedRoundVersion);

public sealed record TechnicalCancelGameRoundRequestDto(
    int ExpectedRoundVersion,
    string ReasonCode,
    string? PublicSummary,
    string InternalDetail
);

public sealed record FinalizeGameRoundModifierRequestDto(
    string ModifierResultId,
    int? CountValue,
    bool? IsConditionMet
);

public sealed record FinalizeGameRoundRuleGroupRequestDto(
    string ResolutionGroupId,
    IReadOnlyList<string> MemberResultIds,
    string OutcomeStatus,
    string? ViolationComment
);

public sealed record FinalizeGameRoundRequestDto(
    string Status,
    int KillsCount,
    int BountyCount,
    string? Notes,
    IReadOnlyList<FinalizeGameRoundModifierRequestDto>? ModifierResults,
    IReadOnlyList<FinalizeGameRoundRuleGroupRequestDto>? RuleGroups = null,
    int? ExpectedRoundVersion = null
);

public sealed record GameRoundScorePreviewDto(
    GameRoundScoreDetailsDto ScoreDetails,
    IReadOnlyList<GameRoundModifierResultDto> ModifierResults,
    int RoundVersion,
    string NormalizedInputHash,
    IReadOnlyList<GameRoundModifierCalculationTraceDto> CalculationTrace
);

public sealed record GameRoundModifierCalculationTraceDto(
    string ModifierResultId,
    string ActivationId,
    string? FormulaCode,
    int? FormulaVersion,
    string ResolutionKind,
    int PointsDelta,
    int BonusKillsDelta
);

public sealed record GameRoundStateChangedEventDto(
    string GameId,
    string RoundId,
    string Status,
    int RoundVersion,
    DateTime OccurredAtUtc
);
