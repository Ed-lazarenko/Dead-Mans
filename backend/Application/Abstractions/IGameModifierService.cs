using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public enum ActivateGameModifierOutcome
{
    Activated,
    UnknownModifierCode,
    GameNotActive,
    ModifierNotEnabled,
    ModifierConflictActive,
    ModifierLimitReached,
    UserNotResolved
}

public sealed record ActivateGameModifierResult(
    ActivateGameModifierOutcome Outcome,
    GameModifierActivatedEvent? Event = null
);

public enum CreateGameModifierOutcome
{
    Created,
    InvalidRequest,
    DuplicateCode
}

public sealed record CreateGameModifierResult(
    CreateGameModifierOutcome Outcome,
    GameModifierDefinition? Modifier = null
);

public enum UpdateGameModifierOutcome
{
    Updated,
    NotFound,
    InvalidRequest
}

public sealed record UpdateGameModifierResult(
    UpdateGameModifierOutcome Outcome,
    GameModifierDefinition? Modifier = null
);

public enum DeleteGameModifierOutcome
{
    Deleted,
    NotFound
}

public sealed record DeleteGameModifierResult(DeleteGameModifierOutcome Outcome);

public interface IGameModifierService
{
    Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    );

    Task<CreateGameModifierResult> CreateAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    Task<UpdateGameModifierResult> UpdateAsync(
        string modifierCode,
        UpdateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    Task<DeleteGameModifierResult> ArchiveAsync(
        string modifierCode,
        CancellationToken cancellationToken = default
    );

    Task<ActivateGameModifierResult> ActivateAsync(
        string modifierCode,
        Guid? activatedByUserId,
        CancellationToken cancellationToken = default
    );
}
