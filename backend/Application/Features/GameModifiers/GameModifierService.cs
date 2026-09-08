using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;

namespace backend.Application.Features.GameModifiers;

public sealed partial class GameModifierService : IGameModifierService
{
    private readonly IGameModifierRepository _repository;
    private readonly IGameNotificationService _notificationService;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GameModifierService> _logger;

    public GameModifierService(
        IGameModifierRepository repository,
        IGameNotificationService notificationService,
        IGameBoardEventsPublisher eventsPublisher,
        TimeProvider timeProvider,
        ILogger<GameModifierService> logger
    )
    {
        _repository = repository;
        _notificationService = notificationService;
        _eventsPublisher = eventsPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task<IReadOnlyList<GameModifierDefinition>> GetCatalogAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetCatalogAsync(cancellationToken);
    }

    public Task<GetGameModifierStateResult> GetStateAsync(
        Guid? userId,
        CancellationToken cancellationToken = default
    )
    {
        return userId.HasValue
            ? GetStateCoreAsync(userId.Value, cancellationToken)
            : Task.FromResult(new GetGameModifierStateResult(
                GetGameModifierStateOutcome.GameNotActive));
    }

    private async Task<GetGameModifierStateResult> GetStateCoreAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _repository.GetStateAsync(userId, cancellationToken);
        return result.Outcome switch
        {
            GetGameModifierStateRepositoryOutcome.Loaded when result.State is not null =>
                new(GetGameModifierStateOutcome.Loaded, result.State),
            GetGameModifierStateRepositoryOutcome.VersionBindingMissing =>
                new(GetGameModifierStateOutcome.VersionBindingMissing),
            _ => new(GetGameModifierStateOutcome.GameNotActive)
        };
    }

    public Task<GameModifierAdminPlayersResult> GetAdminPlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        return _repository.GetAdminPlayersAsync(cancellationToken);
    }

    public async Task<GetAdminGameModifierStateResult> GetAdminStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        if (!await _repository.HasActiveGameAsync(cancellationToken))
        {
            return new GetAdminGameModifierStateResult(GetAdminGameModifierStateOutcome.GameNotActive);
        }

        if (!await _repository.AdminPlayerExistsAsync(userId, cancellationToken))
        {
            return new GetAdminGameModifierStateResult(GetAdminGameModifierStateOutcome.PlayerNotFound);
        }

        var result = await _repository.GetStateAsync(userId, cancellationToken);
        return result.Outcome switch
        {
            GetGameModifierStateRepositoryOutcome.Loaded when result.State is not null =>
                new(GetAdminGameModifierStateOutcome.Loaded, result.State),
            GetGameModifierStateRepositoryOutcome.VersionBindingMissing =>
                new(GetAdminGameModifierStateOutcome.VersionBindingMissing),
            _ => new(GetAdminGameModifierStateOutcome.GameNotActive)
        };
    }

    public async Task<GetAdminActiveGameModifierActivationsResult> GetAdminActiveActivationsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGame = await _repository.HasActiveGameAsync(cancellationToken);
        if (!activeGame)
        {
            return new GetAdminActiveGameModifierActivationsResult(false, []);
        }

        var gameId = await _repository.GetActiveGameIdAsync(cancellationToken);
        if (!gameId.HasValue)
        {
            return new GetAdminActiveGameModifierActivationsResult(false, []);
        }

        var activations = await _repository.GetActiveModifiersForGameAsync(
            gameId.Value,
            cancellationToken
        );
        return new GetAdminActiveGameModifierActivationsResult(true, activations);
    }
}
