using backend.Domain.GameModifiers;

namespace backend.Application.Contracts;

public static class GameModifierCategories
{
    public const string Preparation = "preparation";
    public const string Round = "round";
    public const string Result = "result";
}

public sealed record GameModifierActivationLimit(int? Count);

public sealed record GameModifierDefinition(
    Guid Id,
    string Category,
    string Name,
    string Description,
    int ActivationCost,
    GameModifierActivationLimit ActivationLimit,
    IReadOnlyList<Guid> ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand,
    bool IsLockedByActiveGame,
    int Revision,
    IReadOnlyList<string> NormalizedTags,
    ModifierBehaviorV2 BehaviorV2
);

public sealed record CreateGameModifierInput(
    string Name,
    string Description,
    string Category,
    int ActivationCost,
    GameModifierActivationLimit ActivationLimit,
    IReadOnlyList<Guid> ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand,
    IReadOnlyList<string>? NormalizedTags,
    ModifierBehaviorV2 BehaviorV2
);

public sealed record UpdateGameModifierInput(
    string Name,
    string Description,
    string Category,
    int ActivationCost,
    GameModifierActivationLimit ActivationLimit,
    IReadOnlyList<Guid> ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand,
    IReadOnlyList<string>? NormalizedTags,
    ModifierBehaviorV2 BehaviorV2
);

public sealed record GameModifierDraftExample(
    int CardValue,
    int KillsCount,
    int BountyCount,
    string ResolutionExample,
    int PointsDelta,
    int BonusKillsDelta,
    int FinalScore
);

public sealed record GameModifierDraftPreview(
    string Name,
    string Description,
    string? IconEmoji,
    string ActivationCommand,
    IReadOnlyList<string> NormalizedTags,
    ModifierBehaviorV2 BehaviorV2,
    GameModifierDraftExample Example
);

public sealed record GameModifierActivation(
    Guid ActivationId,
    Guid RoundId,
    int RoundVersion,
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
    int? Limit,
    bool IsEmergencyDisabled,
    DateTime? EmergencyDisabledAtUtc
);

public sealed record EmergencyDisableGameModifierInput(Guid ModifierId, Guid DisabledByUserId, string Reason);

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

public sealed record GameModifierAvailabilityChangedEvent(
    string GameId,
    int Version,
    Guid ModifierId
);
