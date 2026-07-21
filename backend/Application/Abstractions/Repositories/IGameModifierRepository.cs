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
    InsufficientQuizPoints
}

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
    AlreadyAppliedInRound
}

public sealed record CancelGameModifierActivationRepositoryResult(
    CancelGameModifierActivationRepositoryStatus Status,
    string? GameId = null,
    int? Version = null,
    Guid? ActivationId = null
);

public interface IGameModifierRepository
{
    Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameModifierState?> GetStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasActiveGameAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameModifierAdminPlayer>> GetAdminPlayersAsync(
        CancellationToken cancellationToken = default
    );

    Task<bool> AdminPlayerExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<GameModifierDefinition?> CreateModifierAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates an existing, non-archived modifier. Returns null when it does not exist.</summary>
    Task<GameModifierDefinition?> UpdateModifierAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    /// <summary>Soft-deletes (archives) a modifier definition. Returns false when not found.</summary>
    Task<bool> ArchiveModifierAsync(Guid modifierId, CancellationToken cancellationToken = default);

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
        CancellationToken cancellationToken = default
    );

    Task<CancelGameModifierActivationRepositoryResult> CancelActivationAsync(
        Guid activationId,
        CancellationToken cancellationToken = default
    );
}
