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

public sealed record GameModifierDefinition(
    string Code,
    string Kind,
    string Category,
    string ScoringType,
    string Tier,
    string Name,
    string Description,
    int ActivationCost,
    int? DefaultLimitPerGame,
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
    string Code,
    string Name,
    string Description,
    string Kind,
    string Category,
    string ScoringType,
    string Tier,
    int ActivationCost,
    int? DefaultLimitPerGame,
    string? IconEmoji,
    string? ActivationCommand
);

public sealed record UpdateGameModifierInput(
    string Name,
    string Description,
    string Kind,
    string Category,
    string ScoringType,
    string Tier,
    int ActivationCost,
    int? DefaultLimitPerGame,
    string? IconEmoji,
    string? ActivationCommand
);

public sealed record GameModifierActivation(
    string ModifierCode,
    string ActivatedByUserId,
    DateTime ActivatedAtUtc
);

public sealed record GameModifierActivatedEvent(
    string GameId,
    int Version,
    GameModifierActivation Activation
);
