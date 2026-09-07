using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public enum ActivateGameModifierOutcome
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
    UserNotResolved,
    VersionBindingMissing
}

public enum GetGameModifierStateOutcome
{
    Loaded,
    GameNotActive,
    VersionBindingMissing
}

public sealed record GetGameModifierStateResult(
    GetGameModifierStateOutcome Outcome,
    GameModifierState? State = null);

public sealed record ActivateGameModifierResult(
    ActivateGameModifierOutcome Outcome,
    GameModifierActivatedEvent? Event = null
);

public enum GetAdminGameModifierStateOutcome
{
    Loaded,
    GameNotActive,
    PlayerNotFound,
    VersionBindingMissing
}

public sealed record GetAdminGameModifierStateResult(
    GetAdminGameModifierStateOutcome Outcome,
    GameModifierState? State = null
);

public sealed record GetAdminActiveGameModifierActivationsResult(
    bool HasActiveGame,
    IReadOnlyList<GameModifierActivation> Activations
);

public enum CreateGameModifierOutcome
{
    Created,
    InvalidRequest,
    CompatibilityLocked
}

public sealed record CreateGameModifierResult(
    CreateGameModifierOutcome Outcome,
    GameModifierDefinition? Modifier = null
);

public enum PreviewGameModifierOutcome
{
    Previewed,
    InvalidRequest,
    CalculationFailed
}

public sealed record PreviewGameModifierResult(
    PreviewGameModifierOutcome Outcome,
    GameModifierDraftPreview? Preview = null,
    string? ErrorCode = null
);

public enum UpdateGameModifierOutcome
{
    Updated,
    Unchanged,
    NotFound,
    InvalidRequest,
    ContentLocked,
    CompatibilityLocked,
    Stale,
    Archived,
    VersionBindingMissing
}

public sealed record UpdateGameModifierResult(
    UpdateGameModifierOutcome Outcome,
    GameModifierDefinition? Modifier = null
);

public enum DeleteGameModifierOutcome
{
    Deleted,
    NotFound,
    ContentLocked,
    Stale,
    VersionBindingMissing
}

public enum EmergencyDisableGameModifierOutcome
{
    Disabled,
    AlreadyDisabled,
    GameNotActive,
    ModifierNotEnabled,
    InvalidRequest,
    UserNotResolved
}

public sealed record EmergencyDisableGameModifierResult(
    EmergencyDisableGameModifierOutcome Outcome,
    GameModifierAvailabilityChangedEvent? Event = null
);

public sealed record DeleteGameModifierResult(DeleteGameModifierOutcome Outcome);

public enum CancelGameModifierActivationOutcome
{
    Cancelled,
    GameNotActive,
    ActivationNotFound,
    Forbidden,
    InvalidRoundState,
    StaleVersion,
    ReasonRequired,
    UserNotResolved
}

public sealed record CancelGameModifierActivationResult(
    CancelGameModifierActivationOutcome Outcome,
    GameModifierActivationCancelledEvent? Event = null
);

public interface IGameModifierService
{
    Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    );

    Task<GetGameModifierStateResult> GetStateAsync(
        Guid? userId,
        CancellationToken cancellationToken = default
    );

    Task<GameModifierAdminPlayersResult> GetAdminPlayersAsync(
        CancellationToken cancellationToken = default
    );

    Task<GetAdminGameModifierStateResult> GetAdminStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<GetAdminActiveGameModifierActivationsResult> GetAdminActiveActivationsAsync(
        CancellationToken cancellationToken = default
    );

    Task<CreateGameModifierResult> CreateAsync(
        CreateGameModifierInput input,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    );

    Task<PreviewGameModifierResult> PreviewCreateAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    Task<UpdateGameModifierResult> UpdateAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    );

    Task<DeleteGameModifierResult> ArchiveAsync(
        Guid modifierId,
        int expectedRevision,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    );

    Task<ModifierHistoryPage<ModifierHistorySummary>?> GetHistoryAsync(
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

    Task<EmergencyDisableGameModifierResult> EmergencyDisableAsync(
        Guid modifierId,
        Guid? disabledByUserId,
        string? reason,
        CancellationToken cancellationToken = default
    );

    Task<ActivateGameModifierResult> ActivateAsync(
        Guid modifierId,
        Guid? activatedByUserId,
        Guid? initiatedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<CancelGameModifierActivationResult> CancelActivationAsync(
        Guid activationId,
        Guid? cancelledByUserId,
        int expectedRoundVersion,
        bool isAdmin,
        string? reason = null,
        string? cancelledByDisplayName = null,
        CancellationToken cancellationToken = default
    );
}
