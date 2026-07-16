namespace backend.Api.Contracts;

public sealed record GameCardRunParticipantDto(string UserId, string DisplayName);

public sealed record GameCardRunTeamOptionDto(
    string TeamId,
    int TeamSlotIndex,
    IReadOnlyList<GameCardRunParticipantDto> Participants
);

public sealed record GameCardRunModifierResultDto(
    string ModifierResultId,
    string ModifierId,
    string ModifierName,
    string ModifierCategory,
    string ModifierMechanicType,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc
);

public sealed record GameCardRunDetailsDto(
    string CardRunId,
    string GameId,
    string CellId,
    string TeamId,
    int TeamSlotIndex,
    string Status,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    int BaseScore,
    int? FinalScore,
    string? Notes,
    IReadOnlyList<GameCardRunParticipantDto> Participants,
    IReadOnlyList<GameCardRunModifierResultDto> ModifierResults
);

public sealed record StartGameCardRunRequestDto(string CellId, string TeamId);

public sealed record FinalizeGameCardRunModifierRequestDto(
    string ModifierResultId,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson
);

public sealed record FinalizeGameCardRunRequestDto(
    string Status,
    int? FinalScore,
    string? Notes,
    IReadOnlyList<FinalizeGameCardRunModifierRequestDto>? ModifierResults
);
