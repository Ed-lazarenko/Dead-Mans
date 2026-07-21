namespace backend.Api.Contracts;

public sealed record GameModifierActivationLimitDto(int? Count);

public sealed record GameModifierScoreImpactDto(
    int? PointsDelta,
    int? PerKillBonus,
    int? FailurePenaltyPoints,
    decimal? MultiplierDelta,
    int? KillDelta
);

public sealed record GameModifierConditionDto(string Type, string Source);

public sealed record GameModifierKillEffectDto(
    string? KillDeltaMode,
    int? KillDeltaValue,
    string? Condition,
    string[] ExcludedWeapons
);

public sealed record GameModifierMultiplierEffectDto(
    string? Target,
    decimal? Delta,
    string? ActiveWindow,
    string? StopCondition
);

public sealed record GameModifierMentorEffectDto(
    string? LoadoutText,
    int? DurationSeconds,
    bool? CanBeRevived,
    bool? CanBeKilled,
    bool? KillsCreditToTeam
);

public sealed record GameModifierEffectDto(
    string MechanicType,
    string[] Traits,
    int? DurationSeconds,
    string? RuleText,
    GameModifierScoreImpactDto? ScoreImpact,
    GameModifierConditionDto[] Conditions,
    string[] ResolutionInputs,
    GameModifierKillEffectDto? KillEffect,
    GameModifierMultiplierEffectDto? MultiplierEffect,
    GameModifierMentorEffectDto? MentorEffect
);

public sealed record GameModifierDefinitionDto(
    string Id,
    string ScoringType,
    string Category,
    bool RequiresHostControl,
    string MechanicType,
    string Name,
    string Description,
    int ActivationCost,
    int? DefaultLimitPerGame,
    GameModifierActivationLimitDto ActivationLimit,
    GameModifierEffectDto Effect,
    string[] ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand
);

public sealed record CreateGameModifierRequestDto(
    string Name,
    string Description,
    string MechanicType,
    string Category,
    bool RequiresHostControl,
    int ActivationCost,
    GameModifierActivationLimitDto ActivationLimit,
    GameModifierEffectDto Effect,
    string[]? ConflictingModifierIds = null,
    int? DefaultLimitPerGame = null,
    string? ScoringType = null,
    string? IconEmoji = null,
    string? ActivationCommand = null
);

public sealed record UpdateGameModifierRequestDto(
    string Name,
    string Description,
    string MechanicType,
    string Category,
    bool RequiresHostControl,
    int ActivationCost,
    GameModifierActivationLimitDto ActivationLimit,
    GameModifierEffectDto Effect,
    string[]? ConflictingModifierIds = null,
    int? DefaultLimitPerGame = null,
    string? ScoringType = null,
    string? IconEmoji = null,
    string? ActivationCommand = null
);

public sealed record GameModifierActivationDto(
    string ActivationId,
    string ModifierId,
    string ModifierName,
    string ActivatedByUserId,
    string ActivatedByDisplayName,
    int ActivationCost,
    DateTime ActivatedAtUtc
);

public sealed record GameModifierAvailabilityDto(
    GameModifierDefinitionDto Modifier,
    bool IsActive,
    bool CanActivate,
    string? BlockedReason,
    int ActivationsCount,
    int? Limit
);

public sealed record GameModifierStateDto(
    string GameId,
    int AvailableQuizPoints,
    int EarnedQuizPoints,
    int SpentQuizPoints,
    bool IsOrderingOpen,
    IReadOnlyList<GameModifierActivationDto> ActiveModifiers,
    IReadOnlyList<GameModifierAvailabilityDto> AvailableModifiers
);

public sealed record GameModifierAdminPlayerDto(
    string UserId,
    string Login,
    string DisplayName,
    int AvailableQuizPoints,
    int EarnedQuizPoints,
    int SpentQuizPoints
);

public sealed record AdminActivateGameModifierRequestDto(string ModifierId, string TargetUserId);

public sealed record GameModifierActivatedEventDto(
    string GameId,
    int Version,
    GameModifierActivationDto Activation
);

public sealed record GameModifierActivationCancelledEventDto(
    string GameId,
    int Version,
    string ActivationId
);
