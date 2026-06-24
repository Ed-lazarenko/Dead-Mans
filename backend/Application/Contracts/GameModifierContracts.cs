namespace backend.Application.Contracts;

public static class GameModifierKinds
{
    public const string Active = "active";
    public const string Passive = "passive";
}

public static class GameModifierScoringTypes
{
    public const string Multiplier = "multiplier";
    public const string FlatBonus = "flat_bonus";
    public const string FlatPenalty = "flat_penalty";
    public const string PerKillBonus = "per_kill_bonus";
    public const string ConditionalBonus = "conditional_bonus";
    public const string ConditionalPenalty = "conditional_penalty";
    public const string ConditionalBonusPenalty = "conditional_bonus_penalty";
    public const string ReplacementRule = "replacement_rule";
    public const string NonScoring = "non_scoring";
}

public static class GameModifierMechanicTypes
{
    public const string RuleOnly = "rule_only";
    public const string RestrictionWithReward = "restriction_with_reward";
    public const string KillCounter = "kill_counter";
    public const string Multiplier = "multiplier";
    public const string Mentor = "mentor";
}

public static class GameModifierActivationLimitScopes
{
    public const string Game = "game";
    public const string Round = "round";
    public const string Team = "team";
    public const string Player = "player";
}

public sealed record GameModifierActivationLimit(int? Count, string Scope);

public sealed record GameModifierScoreImpact(
    int? PointsDelta,
    int? PerKillBonus,
    int? FailurePenaltyPoints,
    decimal? MultiplierDelta,
    int? KillDelta
);

public sealed record GameModifierCondition(string Type, string Source);

public sealed record GameModifierKillEffect(
    string? KillDeltaMode,
    int? KillDeltaValue,
    string? Condition,
    string[] ExcludedWeapons
);

public sealed record GameModifierMultiplierEffect(
    string? Target,
    decimal? Delta,
    string? ActiveWindow,
    string? StopCondition
);

public sealed record GameModifierMentorEffect(
    string? LoadoutText,
    int? DurationSeconds,
    bool? CanBeRevived,
    bool? CanBeKilled,
    bool? KillsCreditToTeam
);

public sealed record GameModifierEffect(
    string MechanicType,
    string[] Traits,
    int? DurationSeconds,
    string? RuleText,
    GameModifierScoreImpact? ScoreImpact,
    GameModifierCondition[] Conditions,
    string[] ResolutionInputs,
    GameModifierKillEffect? KillEffect,
    GameModifierMultiplierEffect? MultiplierEffect,
    GameModifierMentorEffect? MentorEffect
);

public sealed record GameModifierDefinition(
    Guid Id,
    string Kind,
    string ScoringType,
    string MechanicType,
    string Tier,
    string Name,
    string Description,
    int ActivationCost,
    int? DefaultLimitPerGame,
    GameModifierActivationLimit ActivationLimit,
    GameModifierEffect Effect,
    IReadOnlyList<Guid> ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand
);

public static class GameModifierTiers
{
    public const string Low = "low";
    public const string Mid = "mid";
    public const string High = "high";
}

public sealed record CreateGameModifierInput(
    string Name,
    string Description,
    string Kind,
    string ScoringType,
    string MechanicType,
    string Tier,
    int ActivationCost,
    int? DefaultLimitPerGame,
    GameModifierActivationLimit ActivationLimit,
    GameModifierEffect Effect,
    IReadOnlyList<Guid> ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand
);

public sealed record UpdateGameModifierInput(
    string Name,
    string Description,
    string Kind,
    string ScoringType,
    string MechanicType,
    string Tier,
    int ActivationCost,
    int? DefaultLimitPerGame,
    GameModifierActivationLimit ActivationLimit,
    GameModifierEffect Effect,
    IReadOnlyList<Guid> ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand
);

public sealed record GameModifierActivation(
    Guid ModifierId,
    string ActivatedByUserId,
    DateTime ActivatedAtUtc
);

public sealed record GameModifierActivatedEvent(
    string GameId,
    int Version,
    GameModifierActivation Activation
);
