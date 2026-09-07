using System.Text.Json.Serialization;

namespace backend.Api.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GameModifierRuleStatusResolutionDto), "ruleStatus")]
[JsonDerivedType(typeof(GameModifierBooleanResolutionDto), "boolean")]
[JsonDerivedType(typeof(GameModifierNonNegativeCountResolutionDto), "nonNegativeCount")]
[JsonDerivedType(typeof(GameModifierAutomaticRoundMetricResolutionDto), "automaticRoundMetric")]
[JsonDerivedType(typeof(GameModifierPerActivationResolutionDto), "perActivation")]
public abstract record GameModifierResolutionDto;
public sealed record GameModifierRuleStatusResolutionDto : GameModifierResolutionDto;
public sealed record GameModifierBooleanResolutionDto(string? InputLabel = null)
    : GameModifierResolutionDto;
public sealed record GameModifierNonNegativeCountResolutionDto(
    string? InputLabel = null,
    string? MaximumKind = null,
    int? MaximumPerActivation = null
) : GameModifierResolutionDto;
public sealed record GameModifierAutomaticRoundMetricResolutionDto(string Metric)
    : GameModifierResolutionDto;
public sealed record GameModifierPerActivationResolutionDto : GameModifierResolutionDto;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GameModifierGrowingKillValueParametersDto), "growingKillValue")]
[JsonDerivedType(typeof(GameModifierBonusKillOnConditionParametersDto), "bonusKillOnCondition")]
[JsonDerivedType(typeof(GameModifierBonusKillsByCountParametersDto), "bonusKillsByCount")]
[JsonDerivedType(typeof(GameModifierWindowKillBonusPointsParametersDto), "windowKillBonusPoints")]
[JsonDerivedType(typeof(GameModifierFixedPointsPerUnitParametersDto), "fixedPointsPerUnit")]
[JsonDerivedType(typeof(GameModifierCardPercentPerUnitParametersDto), "cardPercentPerUnit")]
[JsonDerivedType(typeof(GameModifierBonusKillsPerUnitParametersDto), "bonusKillsPerUnit")]
[JsonDerivedType(typeof(GameModifierKillValueIncreasePerUnitParametersDto), "killValueIncreasePerUnit")]
public abstract record GameModifierFormulaParametersDto;
public sealed record GameModifierGrowingKillValueParametersDto(
    int IncrementPointsPerKill,
    int ZeroKillPenaltyPoints
) : GameModifierFormulaParametersDto;
public sealed record GameModifierBonusKillOnConditionParametersDto(int SuccessBonusKills)
    : GameModifierFormulaParametersDto;
public sealed record GameModifierBonusKillsByCountParametersDto(int BonusKillsPerUnit)
    : GameModifierFormulaParametersDto;
public sealed record GameModifierWindowKillBonusPointsParametersDto(decimal BonusRate)
    : GameModifierFormulaParametersDto;
public sealed record GameModifierFixedPointsPerUnitParametersDto(int PointsPerUnit)
    : GameModifierFormulaParametersDto;
public sealed record GameModifierCardPercentPerUnitParametersDto(decimal Rate)
    : GameModifierFormulaParametersDto;
public sealed record GameModifierBonusKillsPerUnitParametersDto(int BonusKillsPerUnit)
    : GameModifierFormulaParametersDto;
public sealed record GameModifierKillValueIncreasePerUnitParametersDto(
    int IncrementPointsPerUnit,
    int ZeroCountPenaltyPoints
) : GameModifierFormulaParametersDto;

public sealed record GameModifierFormulaReferenceV2Dto(
    string Code,
    int Version,
    GameModifierFormulaParametersDto Parameters
);

public sealed record GameModifierBehaviorV2Dto(
    int SchemaVersion,
    string Kind,
    string Phase,
    string Performer,
    bool RequiresHostMonitoring,
    string Rule,
    string StackingPolicy,
    GameModifierResolutionDto Resolution,
    string Reward,
    GameModifierFormulaReferenceV2Dto? FormulaReference,
    int? DurationSecondsPerActivation = null
);

public sealed record GameModifierActivationLimitDto(int? Count);

public sealed record GameModifierDefinitionDto(
    string Id,
    string Category,
    string Name,
    string Description,
    int ActivationCost,
    GameModifierActivationLimitDto ActivationLimit,
    string[] ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand,
    bool IsLockedByActiveGame,
    int Revision,
    string[] NormalizedTags,
    GameModifierBehaviorV2Dto BehaviorV2
);

public sealed record CreateGameModifierRequestDto(
    string Name,
    string Description,
    string Category,
    int ActivationCost,
    GameModifierActivationLimitDto ActivationLimit,
    string[]? ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand,
    string[]? NormalizedTags,
    GameModifierBehaviorV2Dto BehaviorV2,
    string? ChangeNote = null
);

public sealed record UpdateGameModifierRequestDto(
    string Name,
    string Description,
    string Category,
    int ActivationCost,
    GameModifierActivationLimitDto ActivationLimit,
    string[]? ConflictingModifierIds,
    string? IconEmoji,
    string? ActivationCommand,
    string[]? NormalizedTags,
    GameModifierBehaviorV2Dto BehaviorV2,
    int ExpectedRevision = 1,
    string? ChangeNote = null
);

public sealed record ModifierHistoryPageDto<T>(IReadOnlyList<T> Items, string? NextCursor);

public sealed record ModifierHistorySummaryDto(
    string ModifierId,
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

public sealed record ModifierVersionSummaryDto(
    string VersionId,
    string ModifierId,
    int Revision,
    string Name,
    DateTime CreatedAtUtc,
    string? CreatedByUserId,
    string CreatedByDisplayName,
    string? ChangeNote,
    string ChangeType,
    string? CascadeSourceModifierId,
    IReadOnlyList<string> ChangedFields
);

public sealed record ModifierConflictSnapshotDto(string ModifierId, string Name);

public sealed record ModifierVersionDetailDto(
    string VersionId,
    string ModifierId,
    int Revision,
    string Name,
    string Description,
    string Category,
    string? IconEmoji,
    string? ActivationCommand,
    int ActivationCost,
    GameModifierActivationLimitDto ActivationLimit,
    IReadOnlyList<string> NormalizedTags,
    GameModifierBehaviorV2Dto BehaviorV2,
    IReadOnlyList<ModifierConflictSnapshotDto> Conflicts,
    DateTime CreatedAtUtc,
    string? CreatedByUserId,
    string CreatedByDisplayName,
    string? ChangeNote,
    string ChangeType,
    string? CascadeSourceModifierId,
    IReadOnlyList<string> ChangedFields,
    bool IsCurrent,
    bool IsArchived
);

public sealed record ModifierVersionGameSummaryDto(
    string GameId,
    string GameTitle,
    string GameStatus,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    int SuccessfulActivationsCount,
    int CancelledActivationsCount,
    int ResultsCount,
    bool IsEmergencyDisabled
);

public sealed record ModifierCatalogChangedItemDto(string ModifierId, int Revision, bool IsArchived);
public sealed record ModifierCatalogChangedEventDto(
    IReadOnlyList<ModifierCatalogChangedItemDto> Modifiers,
    DateTime OccurredAtUtc
);

public sealed record GameModifierDraftExampleDto(
    int CardValue,
    int KillsCount,
    int BountyCount,
    string ResolutionExample,
    int PointsDelta,
    int BonusKillsDelta,
    int FinalScore
);

public sealed record GameModifierDraftPreviewDto(
    string Name,
    string Description,
    string? IconEmoji,
    string ActivationCommand,
    string[] NormalizedTags,
    GameModifierBehaviorV2Dto BehaviorV2,
    GameModifierDraftExampleDto Example
);

public sealed record GameModifierActivationDto(
    string ActivationId,
    string RoundId,
    int RoundVersion,
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
    int? Limit,
    bool IsEmergencyDisabled,
    DateTime? EmergencyDisabledAtUtc
);

public sealed record EmergencyDisableGameModifierRequestDto(string Reason);

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

public sealed record GameModifierAdminPlayersSummaryDto(
    int PlayersCount,
    int TotalAvailableQuizPoints,
    int TotalEarnedQuizPoints,
    int TotalSpentQuizPoints
);

public sealed record GameModifierAdminPlayersResultDto(
    GameModifierAdminPlayersSummaryDto Summary,
    IReadOnlyList<GameModifierAdminPlayerDto> Players
);

public sealed record AdminActivateGameModifierRequestDto(string ModifierId, string TargetUserId);

public sealed record CancelGameModifierActivationRequestDto(
    int ExpectedRoundVersion,
    string? Reason = null
);

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

public sealed record GameModifierAvailabilityChangedEventDto(
    string GameId,
    int Version,
    string ModifierId
);
