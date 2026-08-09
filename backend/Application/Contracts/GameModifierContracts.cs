namespace backend.Application.Contracts;

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

public static class GameModifierCategories
{
    public const string Preparation = "preparation";
    public const string Round = "round";
    public const string Result = "result";
}

public sealed record GameModifierActivationLimit(int? Count);

public static class GameModifierScoreFormulaModes
{
    public const string FlatPerKill = "flat_per_kill";
    public const string StackingPerKillBonus = "stacking_per_kill_bonus";
    public const string CustomExpression = "custom_expression";
}

public sealed record GameModifierScoreFormula(
    string Mode,
    string? SuccessExpression,
    string? FailureExpression
);

public sealed record GameModifierScoreImpact(
    int? PointsDelta,
    int? PerKillBonus,
    int? FailurePenaltyPoints,
    decimal? MultiplierDelta,
    int? KillDelta,
    GameModifierScoreFormula? ScoreFormula
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
    string ScoringType,
    string Category,
    bool RequiresHostControl,
    string MechanicType,
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

public sealed record CreateGameModifierInput(
    string Name,
    string Description,
    string ScoringType,
    string Category,
    bool RequiresHostControl,
    string MechanicType,
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
    string ScoringType,
    string Category,
    bool RequiresHostControl,
    string MechanicType,
    int ActivationCost,
    int? DefaultLimitPerGame,
    GameModifierActivationLimit ActivationLimit,
    GameModifierEffect Effect,
    IReadOnlyList<Guid> ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand
);

public sealed record GameModifierActivation(
    Guid ActivationId,
    Guid ModifierId,
    string ModifierName,
    string ActivatedByUserId,
    string ActivatedByDisplayName,
    int ActivationCost,
    DateTime ActivatedAtUtc
);

public sealed record GameModifierAvailability(
    GameModifierDefinition Modifier,
    bool IsActive,
    bool CanActivate,
    string? BlockedReason,
    int ActivationsCount,
    int? Limit
);

public sealed record GameModifierState(
    Guid GameId,
    int AvailableQuizPoints,
    int EarnedQuizPoints,
    int SpentQuizPoints,
    bool IsOrderingOpen,
    IReadOnlyList<GameModifierActivation> ActiveModifiers,
    IReadOnlyList<GameModifierAvailability> AvailableModifiers
);

public sealed record GameModifierAdminPlayer(
    Guid UserId,
    string Login,
    string DisplayName,
    int AvailableQuizPoints,
    int EarnedQuizPoints,
    int SpentQuizPoints
);

public sealed record GameModifierAdminPlayersSummary(
    int PlayersCount,
    int TotalAvailableQuizPoints,
    int TotalEarnedQuizPoints,
    int TotalSpentQuizPoints
);

public sealed record GameModifierAdminPlayersResult(
    GameModifierAdminPlayersSummary Summary,
    IReadOnlyList<GameModifierAdminPlayer> Players
);

public sealed record GameModifierActivatedEvent(
    string GameId,
    int Version,
    GameModifierActivation Activation
);

public sealed record GameModifierActivationCancelledEvent(
    string GameId,
    int Version,
    Guid ActivationId
);
