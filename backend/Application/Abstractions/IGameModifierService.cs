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
    InsufficientQuizPoints,
    UserNotResolved
}

public sealed record ActivateGameModifierResult(
    ActivateGameModifierOutcome Outcome,
    GameModifierActivatedEvent? Event = null
);

public enum CreateGameModifierOutcome
{
    Created,
    InvalidRequest
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

    Task<GameModifierState?> GetStateAsync(
        Guid? userId,
        CancellationToken cancellationToken = default
    );

    Task<CreateGameModifierResult> CreateAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    Task<UpdateGameModifierResult> UpdateAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        CancellationToken cancellationToken = default
    );

    Task<DeleteGameModifierResult> ArchiveAsync(
        Guid modifierId,
        CancellationToken cancellationToken = default
    );

    Task<ActivateGameModifierResult> ActivateAsync(
        Guid modifierId,
        Guid? activatedByUserId,
        CancellationToken cancellationToken = default
    );
}
