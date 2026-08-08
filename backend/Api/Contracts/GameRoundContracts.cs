namespace backend.Api.Contracts;

public sealed record GameRoundParticipantDto(string UserId, string DisplayName);

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
    string ModifierMechanicType,
    string ModifierDescription,
    string ModifierScoringType,
    GameModifierEffectDto? ModifierEffect,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc
);

public sealed record GameRoundDetailsDto(
    string RoundId,
    string GameId,
    string CellId,
    string TeamId,
    string? TeamName,
    int TeamSlotIndex,
    string Status,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    int BaseScore,
    int? FinalScore,
    bool EmptyCardPenaltyApplied,
    int KillsCount,
    int BountyCount,
    string? Notes,
    IReadOnlyList<GameRoundParticipantDto> Participants,
    IReadOnlyList<GameRoundModifierResultDto> ModifierResults
);

public sealed record StartGameRoundRequestDto(string CellId, string TeamId);

public sealed record FinalizeGameRoundModifierRequestDto(
    string ModifierResultId,
    string OutcomeStatus,
    int ScoreDelta,
    int KillDelta,
    decimal? MultiplierApplied,
    string? ResolutionDataJson
);

public sealed record FinalizeGameRoundRequestDto(
    string Status,
    int? FinalScore,
    int KillsCount,
    int BountyCount,
    string? Notes,
    IReadOnlyList<FinalizeGameRoundModifierRequestDto>? ModifierResults
);

public sealed record GameRoundStateChangedEventDto(
    string GameId,
    string RoundId,
    string Status,
    DateTime OccurredAtUtc
);
