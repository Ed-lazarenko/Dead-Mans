using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Messaging;

namespace backend.Application.Features.GameModifiers;

public sealed class GameModifierService : IGameModifierService
{
    private readonly IGameModifierRepository _repository;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameModifierService> _logger;

    public GameModifierService(
        IGameModifierRepository repository,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameModifierService> logger
    )
    {
        _repository = repository;
        _eventsPublisher = eventsPublisher;
        _logger = logger;
    }

    public Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetCatalogAsync(cancellationToken);
    }

    public async Task<CreateGameModifierResult> CreateAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameModifierValidator.TryNormalizeCreate(input, out var normalized))
        {
            return new CreateGameModifierResult(CreateGameModifierOutcome.InvalidRequest);
        }

        if (normalized.ConflictingModifierIds.Count > 0
            && !await _repository.ModifierIdsExistAsync(
                normalized.ConflictingModifierIds.ToArray(),
                cancellationToken
            ))
        {
            return new CreateGameModifierResult(CreateGameModifierOutcome.InvalidRequest);
        }

        var created = await _repository.CreateModifierAsync(normalized, cancellationToken);
        return created is null
            ? new CreateGameModifierResult(CreateGameModifierOutcome.InvalidRequest)
            : new CreateGameModifierResult(CreateGameModifierOutcome.Created, created);
    }

    public async Task<UpdateGameModifierResult> UpdateAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameModifierValidator.TryNormalizeUpdate(input, out var normalized))
        {
            return new UpdateGameModifierResult(UpdateGameModifierOutcome.InvalidRequest);
        }

        if (normalized.ConflictingModifierIds.Contains(modifierId)
            || (normalized.ConflictingModifierIds.Count > 0
                && !await _repository.ModifierIdsExistAsync(
                    normalized.ConflictingModifierIds.ToArray(),
                    cancellationToken
                )))
        {
            return new UpdateGameModifierResult(UpdateGameModifierOutcome.InvalidRequest);
        }

        var updated = await _repository.UpdateModifierAsync(modifierId, normalized, cancellationToken);
        return updated is null
            ? new UpdateGameModifierResult(UpdateGameModifierOutcome.NotFound)
            : new UpdateGameModifierResult(UpdateGameModifierOutcome.Updated, updated);
    }

    public async Task<DeleteGameModifierResult> ArchiveAsync(
        Guid modifierId,
        CancellationToken cancellationToken = default
    )
    {
        var archived = await _repository.ArchiveModifierAsync(modifierId, cancellationToken);
        return archived
            ? new DeleteGameModifierResult(DeleteGameModifierOutcome.Deleted)
            : new DeleteGameModifierResult(DeleteGameModifierOutcome.NotFound);
    }

    public async Task<ActivateGameModifierResult> ActivateAsync(
        Guid modifierId,
        Guid? activatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (!await _repository.ModifierIdExistsAsync(modifierId, cancellationToken))
        {
            return new ActivateGameModifierResult(ActivateGameModifierOutcome.NotFound);
        }

        if (activatedByUserId is null)
        {
            return new ActivateGameModifierResult(ActivateGameModifierOutcome.UserNotResolved);
        }

        var activationResult = await _repository.ActivateModifierAsync(
            modifierId,
            activatedByUserId.Value,
            cancellationToken
        );

        var result = activationResult.Status switch
        {
            ActivateGameModifierRepositoryStatus.Activated
                when activationResult.GameId is not null
                    && activationResult.Version.HasValue
                    && activationResult.Activation is not null =>
                new ActivateGameModifierResult(
                    ActivateGameModifierOutcome.Activated,
                    new GameModifierActivatedEvent(
                        activationResult.GameId,
                        activationResult.Version.Value,
                        activationResult.Activation
                    )
                ),
            ActivateGameModifierRepositoryStatus.NotFound =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.NotFound),
            ActivateGameModifierRepositoryStatus.GameNotActive => new ActivateGameModifierResult(
                ActivateGameModifierOutcome.GameNotActive
            ),
            ActivateGameModifierRepositoryStatus.ModifierNotEnabled => new ActivateGameModifierResult(
                ActivateGameModifierOutcome.ModifierNotEnabled
            ),
            ActivateGameModifierRepositoryStatus.ModifierConflictActive =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.ModifierConflictActive),
            ActivateGameModifierRepositoryStatus.ModifierLimitReached => new ActivateGameModifierResult(
                ActivateGameModifierOutcome.ModifierLimitReached
            ),
            _ => new ActivateGameModifierResult(ActivateGameModifierOutcome.GameNotActive)
        };

        if (result.Outcome != ActivateGameModifierOutcome.Activated || result.Event is null)
        {
            return result;
        }

        await RealtimePublishGuard.TryPublishAsync(
            () => _eventsPublisher.PublishModifierActivatedAsync(result.Event, cancellationToken),
            _logger,
            AppMessages.Logs.RealtimeGameModifierActivatedPublishFailed,
            result.Event.Activation.ModifierId
        );

        return result;
    }
}
