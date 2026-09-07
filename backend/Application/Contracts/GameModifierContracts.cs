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
    ModifierBehaviorV2 BehaviorV2,
    string? ChangeNote = null
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
    ModifierBehaviorV2 BehaviorV2,
    int ExpectedRevision = 1,
    string? ChangeNote = null
);

public sealed record ModifierChangeActor(Guid UserId, string DisplayName);

public sealed record ModifierHistoryQuery(
    string? Search,
    string Status,
    string? Cursor,
    int Limit
);

public sealed record ModifierVersionQuery(string? Cursor, int Limit);

public sealed record ModifierHistoryPage<T>(IReadOnlyList<T> Items, string? NextCursor);

public sealed record ModifierHistorySummary(
    Guid ModifierId,
    int CurrentRevision,
    string Name,
    string Category,
    string? IconEmoji,
    int ActivationCost,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime? ArchivedAtUtc,
    int VersionCount,
    int GamesCount,
    int ActivationsCount
);

public sealed record ModifierVersionSummary(
    Guid VersionId,
    Guid ModifierId,
    int Revision,
    string Name,
    DateTime CreatedAtUtc,
    Guid? CreatedByUserId,
    string CreatedByDisplayName,
    string? ChangeNote,
    string ChangeType,
    Guid? CascadeSourceModifierId,
    IReadOnlyList<string> ChangedFields
);

public sealed record ModifierConflictSnapshot(Guid ModifierId, string Name);

public sealed record ModifierVersionDetail(
    Guid VersionId,
    Guid ModifierId,
    int Revision,
    string Name,
    string Description,
    string Category,
    string? IconEmoji,
    string? ActivationCommand,
    int ActivationCost,
    GameModifierActivationLimit ActivationLimit,
    IReadOnlyList<string> NormalizedTags,
    ModifierBehaviorV2 BehaviorV2,
    IReadOnlyList<ModifierConflictSnapshot> Conflicts,
    DateTime CreatedAtUtc,
    Guid? CreatedByUserId,
    string CreatedByDisplayName,
    string? ChangeNote,
    string ChangeType,
    Guid? CascadeSourceModifierId,
    IReadOnlyList<string> ChangedFields,
    bool IsCurrent,
    bool IsArchived
);

public sealed record ModifierVersionGameSummary(
    Guid GameId,
    string GameTitle,
    string GameStatus,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    int SuccessfulActivationsCount,
    int CancelledActivationsCount,
    int ResultsCount,
    bool IsEmergencyDisabled
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

public sealed record ModifierCatalogChangedItem(Guid ModifierId, int Revision, bool IsArchived);
public sealed record ModifierCatalogChangedEvent(
    IReadOnlyList<ModifierCatalogChangedItem> Modifiers,
    DateTime OccurredAtUtc
);
