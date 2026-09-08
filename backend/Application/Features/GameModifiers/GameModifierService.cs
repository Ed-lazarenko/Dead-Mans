using backend.Application.Abstractions;
using backend.Application.Abstractions.Realtime;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Domain.GameModifiers;
using backend.Messaging;

namespace backend.Application.Features.GameModifiers;

public sealed class GameModifierService : IGameModifierService
{
    private readonly IGameModifierRepository _repository;
    private readonly IGameNotificationService _notificationService;
    private readonly IGameBoardEventsPublisher _eventsPublisher;
    private readonly ILogger<GameModifierService> _logger;

    public GameModifierService(
        IGameModifierRepository repository,
        IGameNotificationService notificationService,
        IGameBoardEventsPublisher eventsPublisher,
        ILogger<GameModifierService> logger
    )
    {
        _repository = repository;
        _notificationService = notificationService;
        _eventsPublisher = eventsPublisher;
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

    public async Task<CreateGameModifierResult> CreateAsync(
        CreateGameModifierInput input,
        ModifierChangeActor actor,
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

        var created = await _repository.CreateModifierAsync(normalized, actor, cancellationToken);
        if (created.Status == CreateGameModifierRepositoryStatus.Created
            && created.Changes is { Count: > 0 })
        {
            await PublishCatalogChangedAsync(created.Changes);
        }
        return created.Status switch
        {
            CreateGameModifierRepositoryStatus.Created when created.Modifier is not null =>
                new CreateGameModifierResult(CreateGameModifierOutcome.Created, created.Modifier),
            CreateGameModifierRepositoryStatus.CompatibilityLocked =>
                new CreateGameModifierResult(CreateGameModifierOutcome.CompatibilityLocked),
            _ => new CreateGameModifierResult(CreateGameModifierOutcome.InvalidRequest)
        };
    }

    public async Task<PreviewGameModifierResult> PreviewCreateAsync(
        CreateGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        if (!GameModifierValidator.TryNormalizeCreate(input, out var normalized)
            || normalized.BehaviorV2 is null)
        {
            return new PreviewGameModifierResult(PreviewGameModifierOutcome.InvalidRequest);
        }

        if (normalized.ConflictingModifierIds.Count > 0
            && !await _repository.ModifierIdsExistAsync(
                normalized.ConflictingModifierIds.ToArray(),
                cancellationToken
            ))
        {
            return new PreviewGameModifierResult(PreviewGameModifierOutcome.InvalidRequest);
        }

        var resolutionInput = CreateExampleResolutionInput(normalized.BehaviorV2);
        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 3, 1),
            [
                new ModifierInstanceCalculationInput(
                    new ModifierActivationSnapshotV2(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        1,
                        normalized.Name,
                        normalized.BehaviorV2
                    ),
                    resolutionInput
                )
            ]
        );
        if (!result.IsSuccess)
        {
            return new PreviewGameModifierResult(
                PreviewGameModifierOutcome.CalculationFailed,
                ErrorCode: result.Errors.Count > 0
                    ? result.Errors[0].Code
                    : "modifier_calculation.failed"
            );
        }

        var calculation = result.Calculation!;
        var instance = AssertSingle(calculation.Instances);
        return new PreviewGameModifierResult(
            PreviewGameModifierOutcome.Previewed,
            new GameModifierDraftPreview(
                normalized.Name,
                normalized.Description,
                normalized.IconEmoji,
                normalized.ActivationCommand!,
                normalized.NormalizedTags ?? [],
                normalized.BehaviorV2,
                new GameModifierDraftExample(
                    100,
                    3,
                    1,
                    ToResolutionExample(resolutionInput),
                    instance.PointsDelta,
                    instance.BonusKillsDelta,
                    calculation.FinalScore
                )
            )
        );
    }

    private static ModifierResolutionInput CreateExampleResolutionInput(
        ModifierBehaviorV2 behavior
    ) => behavior.Resolution switch
    {
        RuleStatusResolution => new RuleStatusInput(ModifierRuleOutcome.Completed),
        AutomaticRoundMetricResolution => new AutomaticRoundMetricInput(),
        BooleanResolution => new BooleanInput(true),
        NonNegativeCountResolution value => new NonNegativeCountInput(
            value.MaximumKind == ModifierCountMaximumKinds.Activations
                ? Math.Min(2, value.MaximumPerActivation ?? 1)
                : 2
        ),
        PerActivationResolution => new PerActivationInput(),
        _ => throw new ArgumentOutOfRangeException(nameof(behavior))
    };

    private static string ToResolutionExample(ModifierResolutionInput input) => input switch
    {
        RuleStatusInput => "completed",
        AutomaticRoundMetricInput => "automatic",
        BooleanInput => "succeeded",
        NonNegativeCountInput { Count: var count } => count.ToString(
            System.Globalization.CultureInfo.InvariantCulture
        ),
        PerActivationInput => "perActivation",
        _ => throw new ArgumentOutOfRangeException(nameof(input))
    };

    private static T AssertSingle<T>(IReadOnlyList<T> values)
    {
        if (values.Count != 1)
        {
            throw new InvalidOperationException("Modifier preview must produce exactly one instance.");
        }
        return values[0];
    }

    public async Task<UpdateGameModifierResult> UpdateAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        ModifierChangeActor actor,
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

        var updated = await _repository.UpdateModifierAsync(modifierId, normalized, actor, cancellationToken);
        if (updated.Changes is { Count: > 0 })
        {
            await PublishCatalogChangedAsync(updated.Changes);
        }
        return updated.Status switch
        {
            UpdateGameModifierRepositoryStatus.Updated when updated.Modifier is not null =>
                new UpdateGameModifierResult(UpdateGameModifierOutcome.Updated, updated.Modifier),
            UpdateGameModifierRepositoryStatus.Unchanged when updated.Modifier is not null =>
                new UpdateGameModifierResult(UpdateGameModifierOutcome.Unchanged, updated.Modifier),
            UpdateGameModifierRepositoryStatus.ContentLocked =>
                new UpdateGameModifierResult(UpdateGameModifierOutcome.ContentLocked),
            UpdateGameModifierRepositoryStatus.CompatibilityLocked =>
                new UpdateGameModifierResult(UpdateGameModifierOutcome.CompatibilityLocked),
            UpdateGameModifierRepositoryStatus.Stale =>
                new UpdateGameModifierResult(UpdateGameModifierOutcome.Stale),
            UpdateGameModifierRepositoryStatus.Archived =>
                new UpdateGameModifierResult(UpdateGameModifierOutcome.Archived),
            UpdateGameModifierRepositoryStatus.VersionBindingMissing =>
                new UpdateGameModifierResult(UpdateGameModifierOutcome.VersionBindingMissing),
            _ => new UpdateGameModifierResult(UpdateGameModifierOutcome.NotFound)
        };
    }

    public async Task<DeleteGameModifierResult> ArchiveAsync(
        Guid modifierId,
        int expectedRevision,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    )
    {
        if (expectedRevision <= 0)
        {
            return new DeleteGameModifierResult(DeleteGameModifierOutcome.Stale);
        }
        var archived = await _repository.ArchiveModifierAsync(
            modifierId, expectedRevision, actor, cancellationToken);
        if (archived == ArchiveGameModifierRepositoryStatus.Archived)
        {
            await PublishCatalogChangedAsync(
                [new ModifierCatalogChangedItem(modifierId, expectedRevision, true)]);
        }
        return archived switch
        {
            ArchiveGameModifierRepositoryStatus.Archived =>
                new DeleteGameModifierResult(DeleteGameModifierOutcome.Deleted),
            ArchiveGameModifierRepositoryStatus.ContentLocked =>
                new DeleteGameModifierResult(DeleteGameModifierOutcome.ContentLocked),
            ArchiveGameModifierRepositoryStatus.Stale =>
                new DeleteGameModifierResult(DeleteGameModifierOutcome.Stale),
            ArchiveGameModifierRepositoryStatus.VersionBindingMissing =>
                new DeleteGameModifierResult(DeleteGameModifierOutcome.VersionBindingMissing),
            _ => new DeleteGameModifierResult(DeleteGameModifierOutcome.NotFound)
        };
    }

    public Task<ModifierHistoryPage<ModifierHistorySummary>?> GetHistoryAsync(
        ModifierHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        if ((query.Search?.Length ?? 0) > 100
            || (query.Cursor?.Length ?? 0) > 512
            || query.Limit is < 1 or > 100
            || query.Status is not ("active" or "archived" or "all"))
        {
            return Task.FromResult<ModifierHistoryPage<ModifierHistorySummary>?>(null);
        }
        return GetHistoryCoreAsync(query, cancellationToken);
    }

    private async Task<ModifierHistoryPage<ModifierHistorySummary>?> GetHistoryCoreAsync(
        ModifierHistoryQuery query, CancellationToken cancellationToken) =>
        await _repository.GetHistoryAsync(query, cancellationToken);

    public Task<ModifierHistoryPage<ModifierVersionSummary>?> GetVersionsAsync(
        Guid modifierId, ModifierVersionQuery query, CancellationToken cancellationToken = default) =>
        IsValidPage(query) ? _repository.GetVersionsAsync(modifierId, query, cancellationToken)
            : Task.FromResult<ModifierHistoryPage<ModifierVersionSummary>?>(null);

    public Task<ModifierVersionDetail?> GetVersionAsync(
        Guid modifierId, int revision, CancellationToken cancellationToken = default) =>
        revision > 0 ? _repository.GetVersionAsync(modifierId, revision, cancellationToken)
            : Task.FromResult<ModifierVersionDetail?>(null);

    public Task<ModifierHistoryPage<ModifierVersionGameSummary>?> GetVersionGamesAsync(
        Guid modifierId, int revision, ModifierVersionQuery query,
        CancellationToken cancellationToken = default) =>
        revision > 0 && IsValidPage(query)
            ? _repository.GetVersionGamesAsync(modifierId, revision, query, cancellationToken)
            : Task.FromResult<ModifierHistoryPage<ModifierVersionGameSummary>?>(null);

    private static bool IsValidPage(ModifierVersionQuery query) =>
        query.Limit is >= 1 and <= 100 && (query.Cursor?.Length ?? 0) <= 512;

    private Task PublishCatalogChangedAsync(IReadOnlyList<ModifierCatalogChangedItem> changes) =>
        RealtimePublishGuard.TryPublishAsync(
            token => _eventsPublisher.PublishModifierCatalogChangedAsync(
                new ModifierCatalogChangedEvent(changes, DateTime.UtcNow), token),
            _logger,
            "Failed to publish modifier catalog changed realtime event.");

    public async Task<EmergencyDisableGameModifierResult> EmergencyDisableAsync(
        Guid modifierId,
        Guid? disabledByUserId,
        string? reason,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedReason = reason?.Trim();
        if (disabledByUserId is null)
        {
            return new EmergencyDisableGameModifierResult(
                EmergencyDisableGameModifierOutcome.UserNotResolved
            );
        }

        if (string.IsNullOrWhiteSpace(normalizedReason) || normalizedReason.Length > 1000)
        {
            return new EmergencyDisableGameModifierResult(
                EmergencyDisableGameModifierOutcome.InvalidRequest
            );
        }

        var repositoryResult = await _repository.EmergencyDisableModifierAsync(
            new EmergencyDisableGameModifierInput(
                modifierId,
                disabledByUserId.Value,
                normalizedReason
            ),
            cancellationToken
        );
        var result = repositoryResult.Status switch
        {
            EmergencyDisableGameModifierRepositoryStatus.Disabled
                when repositoryResult.GameId is not null
                    && repositoryResult.Version.HasValue
                    && repositoryResult.ModifierId.HasValue =>
                new EmergencyDisableGameModifierResult(
                    EmergencyDisableGameModifierOutcome.Disabled,
                    new GameModifierAvailabilityChangedEvent(
                        repositoryResult.GameId,
                        repositoryResult.Version.Value,
                        repositoryResult.ModifierId.Value
                    )
                ),
            EmergencyDisableGameModifierRepositoryStatus.AlreadyDisabled =>
                new EmergencyDisableGameModifierResult(
                    EmergencyDisableGameModifierOutcome.AlreadyDisabled
                ),
            EmergencyDisableGameModifierRepositoryStatus.GameNotActive =>
                new EmergencyDisableGameModifierResult(
                    EmergencyDisableGameModifierOutcome.GameNotActive
                ),
            _ => new EmergencyDisableGameModifierResult(
                EmergencyDisableGameModifierOutcome.ModifierNotEnabled
            )
        };

        if (result.Outcome != EmergencyDisableGameModifierOutcome.Disabled || result.Event is null)
        {
            return result;
        }

        await RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishModifierAvailabilityChangedAsync(
                result.Event,
                publishToken
            ),
            _logger,
            AppMessages.Logs.RealtimeGameModifierAvailabilityChangedPublishFailed,
            result.Event.ModifierId
        );

        return result;
    }

    public async Task<ActivateGameModifierResult> ActivateAsync(
        Guid modifierId,
        Guid? activatedByUserId,
        Guid? initiatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        if (!await _repository.ModifierIdExistsAsync(modifierId, cancellationToken))
        {
            return new ActivateGameModifierResult(ActivateGameModifierOutcome.NotFound);
        }

        if (activatedByUserId is null || initiatedByUserId is null)
        {
            return new ActivateGameModifierResult(ActivateGameModifierOutcome.UserNotResolved);
        }

        var activationResult = await _repository.ActivateModifierAsync(
            modifierId,
            activatedByUserId.Value,
            initiatedByUserId.Value,
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
            ActivateGameModifierRepositoryStatus.ModifierOrderingClosed =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.ModifierOrderingClosed),
            ActivateGameModifierRepositoryStatus.ActiveTeamMember =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.ActiveTeamMember),
            ActivateGameModifierRepositoryStatus.InsufficientQuizPoints =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.InsufficientQuizPoints),
            ActivateGameModifierRepositoryStatus.EmergencyDisabled =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.EmergencyDisabled),
            ActivateGameModifierRepositoryStatus.VersionBindingMissing =>
                new ActivateGameModifierResult(ActivateGameModifierOutcome.VersionBindingMissing),
            _ => new ActivateGameModifierResult(ActivateGameModifierOutcome.GameNotActive)
        };

        if (result.Outcome != ActivateGameModifierOutcome.Activated || result.Event is null)
        {
            return result;
        }

        await RealtimePublishGuard.TryPublishAsync(
            publishToken => _eventsPublisher.PublishModifierActivatedAsync(result.Event, publishToken),
            _logger,
            AppMessages.Logs.RealtimeGameModifierActivatedPublishFailed,
            result.Event.Activation.ModifierId
        );

        return result;
    }

    public async Task<CancelGameModifierActivationResult> CancelActivationAsync(
        Guid activationId,
        Guid? cancelledByUserId,
        int expectedRoundVersion,
        bool isAdmin,
        string? reason = null,
        string? cancelledByDisplayName = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!cancelledByUserId.HasValue)
        {
            return new CancelGameModifierActivationResult(
                CancelGameModifierActivationOutcome.UserNotResolved
            );
        }

        var cancellationResult = await _repository.CancelActivationAsync(
            new CancelGameModifierActivationRepositoryInput(
                activationId,
                cancelledByUserId.Value,
                expectedRoundVersion,
                isAdmin,
                reason
            ),
            cancellationToken
        );

        var result = cancellationResult.Status switch
        {
            CancelGameModifierActivationRepositoryStatus.Cancelled
                when cancellationResult.GameId is not null
                    && cancellationResult.Version.HasValue
                    && cancellationResult.ActivationId.HasValue
                    && cancellationResult.StateChanged =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.Cancelled,
                    new GameModifierActivationCancelledEvent(
                        cancellationResult.GameId,
                        cancellationResult.Version.Value,
                        cancellationResult.ActivationId.Value
                    )
                ),
            CancelGameModifierActivationRepositoryStatus.Cancelled =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.Cancelled
                ),
            CancelGameModifierActivationRepositoryStatus.GameNotActive =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.GameNotActive
                ),
            CancelGameModifierActivationRepositoryStatus.Forbidden =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.Forbidden
                ),
            CancelGameModifierActivationRepositoryStatus.InvalidRoundState =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.InvalidRoundState
                ),
            CancelGameModifierActivationRepositoryStatus.StaleVersion =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.StaleVersion
                ),
            CancelGameModifierActivationRepositoryStatus.ReasonRequired =>
                new CancelGameModifierActivationResult(
                    CancelGameModifierActivationOutcome.ReasonRequired
                ),
            _ => new CancelGameModifierActivationResult(
                CancelGameModifierActivationOutcome.ActivationNotFound
            )
        };

        if (result.Outcome != CancelGameModifierActivationOutcome.Cancelled)
        {
            return result;
        }

        if (result.Event is not null)
        {
            await RealtimePublishGuard.TryPublishAsync(
                publishToken => _eventsPublisher.PublishModifierActivationCancelledAsync(
                    result.Event,
                    publishToken
                ),
                _logger,
                AppMessages.Logs.RealtimeGameModifierCancelledPublishFailed,
                result.Event.ActivationId
            );
        }

        if (cancellationResult.StateChanged
            && cancellationResult.ActivatedByUserId.HasValue
            && cancellationResult.ActivationId.HasValue
            && Guid.TryParse(cancellationResult.GameId, out var notificationGameId)
            && !string.IsNullOrWhiteSpace(cancellationResult.ModifierName)
            && cancellationResult.RefundedQuizPoints.HasValue)
        {
            await _notificationService.NotifyModifierCancelledAsync(
                cancellationResult.ActivatedByUserId.Value,
                notificationGameId,
                cancellationResult.ActivationId.Value,
                cancellationResult.ModifierName,
                string.IsNullOrWhiteSpace(cancelledByDisplayName)
                    ? "Administrator"
                    : cancelledByDisplayName,
                cancellationResult.RefundedQuizPoints.Value,
                cancellationToken
            );
        }

        return result;
    }
}
