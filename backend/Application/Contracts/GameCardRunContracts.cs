using backend.Domain.Persistence;

namespace backend.Application.Contracts;

public sealed record GameCardRunParticipantSnapshot(Guid UserId, string DisplayName);

public sealed record GameCardRunModifierSnapshot(
    Guid ModifierResultId,
    Guid ModifierId,
    string ModifierName,
    string ModifierCategory,
    string ModifierMechanicType,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson,
    Guid? ResolvedByUserId,
    DateTime? ResolvedAtUtc
);

public sealed record GameCardRunDetails(
    Guid CardRunId,
    Guid GameId,
    Guid CellId,
    Guid TeamId,
    int TeamSlotIndex,
    string Status,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    int BaseScore,
    int? FinalScore,
    string? Notes,
    IReadOnlyList<GameCardRunParticipantSnapshot> Participants,
    IReadOnlyList<GameCardRunModifierSnapshot> ModifierResults
);

public sealed record StartGameCardRunInput(Guid CellId, Guid TeamId);

public sealed record FinalizeGameCardRunModifierInput(
    Guid ModifierResultId,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson
);

public sealed record FinalizeGameCardRunInput(
    string Status,
    int? FinalScore,
    string? Notes,
    IReadOnlyList<FinalizeGameCardRunModifierInput> ModifierResults
);

public enum StartGameCardRunOutcome
{
    Started,
    NoActiveGame,
    CellNotFound,
    CellNotOpen,
    TeamNotFound,
    TeamNotConfirmed,
    TeamHasNoActiveMembers,
    RunAlreadyInProgress,
}

public enum FinalizeGameCardRunOutcome
{
    Completed,
    NotFound,
    NotInProgress,
    InvalidStatus,
    ModifierResultNotFound,
}

public sealed record StartGameCardRunResult(
    StartGameCardRunOutcome Outcome,
    GameCardRunDetails? Run
);

public sealed record FinalizeGameCardRunResult(
    FinalizeGameCardRunOutcome Outcome,
    GameCardRunDetails? Run
)
{
    public static readonly IReadOnlySet<string> AllowedTerminalStatuses = new HashSet<string>(
        [GameCardRunStatusValue.Completed, GameCardRunStatusValue.Cancelled],
        StringComparer.Ordinal
    );
}
