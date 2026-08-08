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
    string ModifierMechanicType,
    string ModifierDescription,
    string ModifierScoringType,
    GameModifierEffect? ModifierEffect,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAtUtc
);

public sealed record GameRoundDetails(
    Guid RoundId,
    Guid GameId,
    Guid CellId,
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
    string? Notes,
    IReadOnlyList<GameRoundParticipantSnapshot> Participants,
    IReadOnlyList<GameRoundModifierSnapshot> ModifierResults
);

public sealed record StartGameRoundInput(Guid CellId, Guid TeamId);

public sealed record FinalizeGameRoundModifierInput(
    Guid ModifierResultId,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson
);

public sealed record FinalizeGameRoundInput(
    string Status,
    int? FinalScore,
    int KillsCount,
    int BountyCount,
    string? Notes,
    IReadOnlyList<FinalizeGameRoundModifierInput> ModifierResults
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
}

public sealed record StartGameRoundResult(
    StartGameRoundOutcome Outcome,
    GameRoundDetails? Round
);

public enum ReviewGameRoundOutcome
{
    Reviewed,
    NotFound,
    NotInProgress,
}

public sealed record ReviewGameRoundResult(
    ReviewGameRoundOutcome Outcome,
    GameRoundDetails? Round
);

public sealed record FinalizeGameRoundResult(
    FinalizeGameRoundOutcome Outcome,
    GameRoundDetails? Round
)
{
    public static readonly IReadOnlySet<string> AllowedTerminalStatuses = new HashSet<string>(
        [GameRoundStatusValue.Completed, GameRoundStatusValue.Cancelled],
        StringComparer.Ordinal
    );
}

public sealed record GameRoundStateChangedEvent(
    Guid GameId,
    Guid RoundId,
    string Status,
    DateTime OccurredAtUtc
);
