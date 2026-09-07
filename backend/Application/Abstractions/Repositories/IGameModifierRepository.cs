using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public enum ActivateGameModifierRepositoryStatus
{
    Activated,
    NotFound,
    GameNotActive,
    ModifierNotEnabled,
    ModifierConflictActive,
    ModifierLimitReached,
    ModifierOrderingClosed,
    ActiveTeamMember,
    InsufficientQuizPoints,
    EmergencyDisabled,
    VersionBindingMissing
}

public enum GetGameModifierStateRepositoryOutcome
{
    Loaded,
    GameNotActive,
    VersionBindingMissing
}

public sealed record GetGameModifierStateRepositoryResult(
    GetGameModifierStateRepositoryOutcome Outcome,
    GameModifierState? State = null);

public enum UpdateGameModifierRepositoryStatus
{
    Updated,
    Unchanged,
    NotFound,
    ContentLocked,
    CompatibilityLocked,
    Stale,
    Archived,
    VersionBindingMissing
}

public sealed record UpdateGameModifierRepositoryResult(
    UpdateGameModifierRepositoryStatus Status,
    GameModifierDefinition? Modifier = null,
    IReadOnlyList<ModifierCatalogChangedItem>? Changes = null
);

public enum CreateGameModifierRepositoryStatus
{
    Created,
    InvalidRequest,
    CompatibilityLocked
}

public sealed record CreateGameModifierRepositoryResult(
    CreateGameModifierRepositoryStatus Status,
    GameModifierDefinition? Modifier = null,
    IReadOnlyList<ModifierCatalogChangedItem>? Changes = null
);

public enum ArchiveGameModifierRepositoryStatus
{
    Archived,
    NotFound,
    ContentLocked,
    Stale,
    VersionBindingMissing
}

public enum EmergencyDisableGameModifierRepositoryStatus
{
    Disabled,
    AlreadyDisabled,
    GameNotActive,
    ModifierNotEnabled
}

public sealed record EmergencyDisableGameModifierRepositoryResult(
    EmergencyDisableGameModifierRepositoryStatus Status,
    string? GameId = null,
    int? Version = null,
    Guid? ModifierId = null
);

public sealed record ActivateGameModifierRepositoryResult(
    ActivateGameModifierRepositoryStatus Status,
    string? GameId = null,
    int? Version = null,
    GameModifierActivation? Activation = null
);

public enum CancelGameModifierActivationRepositoryStatus
{
    Cancelled,
    GameNotActive,
    ActivationNotFound,
    Forbidden,
    InvalidRoundState,
    StaleVersion,
    ReasonRequired
}

public sealed record CancelGameModifierActivationRepositoryInput(
    Guid ActivationId,
    Guid CancelledByUserId,
    int ExpectedRoundVersion,
    bool IsAdmin,
    string? Reason
);

public sealed record CancelGameModifierActivationRepositoryResult(
    CancelGameModifierActivationRepositoryStatus Status,
    string? GameId = null,
    int? Version = null,
    Guid? ActivationId = null,
    Guid? ActivatedByUserId = null,
    string? ModifierName = null,
    int? RefundedQuizPoints = null,
    bool StateChanged = false,
    int? RoundVersion = null
);

public interface IGameModifierRepository
{
    Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    );

    Task<GetGameModifierStateRepositoryResult> GetStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasActiveGameAsync(CancellationToken cancellationToken = default);

    Task<Guid?> GetActiveGameIdAsync(CancellationToken cancellationToken = default);

    Task<GameModifierAdminPlayersResult> GetAdminPlayersAsync(
        CancellationToken cancellationToken = default
    );

    Task<bool> AdminPlayerExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CreateGameModifierRepositoryResult> CreateModifierAsync(
        CreateGameModifierInput input,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    );

    Task<UpdateGameModifierRepositoryResult> UpdateModifierAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    );

    Task<ArchiveGameModifierRepositoryStatus> ArchiveModifierAsync(
        Guid modifierId,
        int expectedRevision,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default);

    Task<ModifierHistoryPage<ModifierHistorySummary>> GetHistoryAsync(
        ModifierHistoryQuery query,
        CancellationToken cancellationToken = default);

    Task<ModifierHistoryPage<ModifierVersionSummary>?> GetVersionsAsync(
        Guid modifierId,
        ModifierVersionQuery query,
        CancellationToken cancellationToken = default);

    Task<ModifierVersionDetail?> GetVersionAsync(
        Guid modifierId,
        int revision,
        CancellationToken cancellationToken = default);

    Task<ModifierHistoryPage<ModifierVersionGameSummary>?> GetVersionGamesAsync(
        Guid modifierId,
        int revision,
        ModifierVersionQuery query,
        CancellationToken cancellationToken = default);

    Task<EmergencyDisableGameModifierRepositoryResult> EmergencyDisableModifierAsync(
        EmergencyDisableGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    Task<bool> ModifierIdExistsAsync(Guid modifierId, CancellationToken cancellationToken = default);

    Task<bool> ModifierIdsExistAsync(
        IReadOnlyList<Guid> modifierIds,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Guid>> GetEnabledModifierIdsForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<GameModifierActivation>> GetActiveModifiersForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<ActivateGameModifierRepositoryResult> ActivateModifierAsync(
        Guid modifierId,
        Guid activatedByUserId,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<CancelGameModifierActivationRepositoryResult> CancelActivationAsync(
        CancelGameModifierActivationRepositoryInput input,
        CancellationToken cancellationToken = default
    );
}
