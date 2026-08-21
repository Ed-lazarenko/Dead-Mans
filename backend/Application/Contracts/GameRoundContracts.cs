using backend.Domain.GameModifiers;
using backend.Domain.Persistence;

namespace backend.Application.Contracts;

public sealed record GameRoundParticipantSnapshot(Guid UserId, string DisplayName);

public sealed record GameRoundTeamOption(
    Guid TeamId,
    string? TeamName,
    int TeamSlotIndex,
    IReadOnlyList<GameRoundParticipantSnapshot> Participants
);

public sealed record GameRoundModifierSnapshot(
    Guid ModifierResultId,
    Guid ModifierId,
    string ModifierName,
    string ModifierCategory,
    string ModifierDescription,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    Guid GameModifierActivationId,
    int DefinitionRevision,
    Guid? ResolutionGroupId,
    string? ResolutionKind,
    string? ViolationComment,
    ModifierBehaviorV2? RuntimeBehavior
);

public sealed record GameRoundScoreDetails(
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

public sealed record GameRoundDetails(
    Guid RoundId,
    Guid GameId,
    Guid CellId,
    string? CellTitle,
    string? CellDescription,
    Guid TeamId,
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
    GameRoundScoreDetails ScoreDetails,
    int KillsCount,
    int BountyCount,
    string? Notes,
    string? TechnicalCancellationReasonCode,
    string? PublicCancellationSummary,
    DateTime ServerNowUtc,
    IReadOnlyList<GameRoundParticipantSnapshot> Participants,
    IReadOnlyList<GameRoundModifierSnapshot> ModifierResults
);

public sealed record StartGameRoundInput(Guid CellId, Guid TeamId);

public sealed record GameRoundVersionCommandInput(int ExpectedRoundVersion);

public sealed record TechnicalCancelGameRoundInput(
    int ExpectedRoundVersion,
    string ReasonCode,
    string? PublicSummary,
    string InternalDetail
);

public sealed record FinalizeGameRoundModifierInput(
    Guid ModifierResultId,
    int? CountValue,
    bool? IsConditionMet
);

public sealed record FinalizeGameRoundRuleGroupInput(
    Guid ResolutionGroupId,
    IReadOnlyList<Guid> MemberResultIds,
    string OutcomeStatus,
    string? ViolationComment
);

public sealed record FinalizeGameRoundInput(
    string Status,
    int KillsCount,
    int BountyCount,
    string? Notes,
    IReadOnlyList<FinalizeGameRoundModifierInput> ModifierResults,
    IReadOnlyList<FinalizeGameRoundRuleGroupInput> RuleGroups,
    int? ExpectedRoundVersion
);

public enum StartGameRoundOutcome
{
    Started,
    NoActiveGame,
    CellNotFound,
    CellNotOpen,
    TeamNotFound,
    TeamNotConfirmed,
    TeamHasNoActiveMembers,
    AwaitingModifiersRequired,
    RoundAlreadyInProgress,
}

public enum FinalizeGameRoundOutcome
{
    Completed,
    NotFound,
    NotInProgress,
    InvalidStatus,
    ModifierResultNotFound,
    InvalidModifierResults,
    StaleVersion,
    CalculationFailed,
}

public sealed record StartGameRoundResult(
    StartGameRoundOutcome Outcome,
    GameRoundDetails? Round
);

public enum TransitionGameRoundOutcome
{
    Transitioned,
    NotFound,
    InvalidState,
    StaleVersion,
    InvalidRequest,
}

public sealed record TransitionGameRoundResult(
    TransitionGameRoundOutcome Outcome,
    GameRoundDetails? Round
);

public sealed record FinalizeGameRoundResult(
    FinalizeGameRoundOutcome Outcome,
    GameRoundDetails? Round,
    string? ErrorCode = null
)
{
    public static readonly IReadOnlySet<string> AllowedTerminalStatuses = new HashSet<string>(
        [GameRoundStatusValue.Completed, GameRoundStatusValue.Cancelled],
        StringComparer.Ordinal
    );

    public static readonly IReadOnlySet<string> AllowedFinalizeStatuses = new HashSet<string>(
        [GameRoundStatusValue.Completed],
        StringComparer.Ordinal
    );
}

public sealed record GameRoundStateChangedEvent(
    Guid GameId,
    Guid RoundId,
    string Status,
    int RoundVersion,
    DateTime OccurredAtUtc
);

public sealed record PreviewGameRoundScoreResult(
    FinalizeGameRoundOutcome Outcome,
    GameRoundScoreDetails? ScoreDetails,
    IReadOnlyList<GameRoundModifierSnapshot> ModifierResults,
    int? RoundVersion = null,
    string? NormalizedInputHash = null,
    IReadOnlyList<GameRoundModifierCalculationTrace>? CalculationTrace = null,
    string? ErrorCode = null
);

public sealed record GameRoundModifierCalculationTrace(
    Guid ModifierResultId,
    Guid ActivationId,
    string? FormulaCode,
    int? FormulaVersion,
    string ResolutionKind,
    int PointsDelta,
    int BonusKillsDelta
);
