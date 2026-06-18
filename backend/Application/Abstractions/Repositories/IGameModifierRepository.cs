using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public enum ActivateGameModifierRepositoryStatus
{
    Activated,
    UnknownModifierCode,
    GameNotActive,
    ModifierNotEnabled,
    ModifierConflictActive,
    ModifierLimitReached
}

public sealed record ActivateGameModifierRepositoryResult(
    ActivateGameModifierRepositoryStatus Status,
    string? GameId = null,
    int? Version = null,
    GameModifierActivation? Activation = null
);

public interface IGameModifierRepository
{
    Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Persists a new modifier definition. Returns null when the code already exists.</summary>
    Task<GameModifierDefinition?> CreateModifierAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates an existing, non-archived modifier. Returns null when it does not exist.</summary>
    Task<GameModifierDefinition?> UpdateModifierAsync(
        string modifierCode,
        UpdateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    /// <summary>Soft-deletes (archives) a modifier definition. Returns false when not found.</summary>
    Task<bool> ArchiveModifierAsync(string modifierCode, CancellationToken cancellationToken = default);

    Task<bool> ModifierCodeExistsAsync(string modifierCode, CancellationToken cancellationToken = default);

    Task<bool> ModifierCodesExistAsync(
        IReadOnlyList<string> modifierCodes,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<string>> GetEnabledModifierCodesForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<GameModifierActivation>> GetActiveModifiersForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<ActivateGameModifierRepositoryResult> ActivateModifierAsync(
        string modifierCode,
        Guid activatedByUserId,
        CancellationToken cancellationToken = default
    );
}
