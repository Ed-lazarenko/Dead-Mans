using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Application.Realtime;
using backend.Domain.GameModifiers;

namespace backend.Application.Features.GameModifiers;

public sealed partial class GameModifierService
{
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

    private async Task<ModifierHistoryPage<ModifierHistorySummary>?> GetHistoryCoreAsync(
        ModifierHistoryQuery query, CancellationToken cancellationToken) =>
        await _repository.GetHistoryAsync(query, cancellationToken);

    private static bool IsValidPage(ModifierVersionQuery query) =>
        query.Limit is >= 1 and <= 100 && (query.Cursor?.Length ?? 0) <= 512;

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

    private Task PublishCatalogChangedAsync(IReadOnlyList<ModifierCatalogChangedItem> changes) =>
        RealtimePublishGuard.TryPublishAsync(
            token => _eventsPublisher.PublishModifierCatalogChangedAsync(
                new ModifierCatalogChangedEvent(
                    changes,
                    _timeProvider.GetUtcNow().UtcDateTime
                ),
                token
            ),
            _logger,
            "Failed to publish modifier catalog changed realtime event.");
}
