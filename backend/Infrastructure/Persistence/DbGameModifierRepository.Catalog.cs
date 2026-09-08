using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameModifierRepository : IGameModifierRepository
{
    public async Task<CreateGameModifierRepositoryResult> CreateModifierAsync(
        CreateGameModifierInput input,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await AcquireCatalogMutationLockAsync(cancellationToken);
        var resolvedActor = await ResolveActorAsync(actor, cancellationToken);
        if (resolvedActor is null)
        {
            return new(CreateGameModifierRepositoryStatus.InvalidRequest);
        }
        var conflictDefinitions = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .Where(x => input.ConflictingModifierIds.Contains(x.Id) && !x.IsArchived)
            .ToArrayAsync(cancellationToken);
        if (conflictDefinitions.Length != input.ConflictingModifierIds.Distinct().Count()
            || conflictDefinitions.Any(x => x.CurrentVersion is null))
        {
            return new(CreateGameModifierRepositoryStatus.InvalidRequest);
        }
        if (await IsAnyContentLockedAsync(input.ConflictingModifierIds, cancellationToken))
        {
            return new(CreateGameModifierRepositoryStatus.CompatibilityLocked);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var entity = new ModifierDefinition
        {
            Id = Guid.NewGuid(),
            IsArchived = false,
            CreatedByUserId = resolvedActor.UserId,
            CreatedAtUtc = now
        };

        _dbContext.ModifierDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var conflictNames = conflictDefinitions.ToDictionary(x => x.Id, x => x.CurrentVersion!.Name);
        var version = ModifierVersionProjector.CreateVersion(
            entity, 1, input, resolvedActor, ModifierVersionChangeTypeValue.Created,
            null, now, conflictNames);
        _dbContext.ModifierDefinitionVersions.Add(version);
        ModifierVersionProjector.ApplyCurrentProjection(entity, version);

        var changes = new List<ModifierCatalogChangedItem>
        {
            new(entity.Id, version.Revision, false)
        };
        var cascadingIds = conflictDefinitions.Select(x => x.Id).ToArray();
        var currentVersionIds = conflictDefinitions.Select(x => x.CurrentVersionId!.Value).ToArray();
        var currentConflictRows = await _dbContext.ModifierDefinitionVersionConflicts.AsNoTracking()
            .Where(x => currentVersionIds.Contains(x.ModifierVersionId))
            .Select(x => new
            {
                ModifierId = x.ModifierVersion.ModifierId,
                ConflictsWithModifierId = x.ConflictingModifierId
            })
            .ToArrayAsync(cancellationToken);
        var cascadeConflictIds = currentConflictRows
            .SelectMany(x => new[] { x.ModifierId, x.ConflictsWithModifierId })
            .Append(entity.Id).Distinct().ToArray();
        var cascadeNames = await _dbContext.ModifierDefinitions.AsNoTracking()
            .Where(x => cascadeConflictIds.Contains(x.Id) && x.CurrentVersionId != null)
            .Select(x => new { x.Id, Name = x.CurrentVersion!.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        cascadeNames[entity.Id] = version.Name;
        foreach (var other in conflictDefinitions.OrderBy(x => x.Id))
        {
            var existingIds = currentConflictRows
                .Where(x => x.ModifierId == other.Id || x.ConflictsWithModifierId == other.Id)
                .Select(x => x.ModifierId == other.Id ? x.ConflictsWithModifierId : x.ModifierId)
                .Distinct().Order().ToArray();
            var nextIds = existingIds.Append(entity.Id).Distinct().Order().ToArray();
            var cascade = CreateVersionForCurrentContent(
                other, nextIds, resolvedActor, ModifierVersionChangeTypeValue.CompatibilityCascade,
                entity.Id, null, now, cascadeNames);
            changes.Add(new(other.Id, cascade.Revision, other.IsArchived));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new CreateGameModifierRepositoryResult(
            CreateGameModifierRepositoryStatus.Created,
            MapDefinition(version, input.ConflictingModifierIds),
            changes);
    }

    public async Task<UpdateGameModifierRepositoryResult> UpdateModifierAsync(
        Guid modifierId,
        UpdateGameModifierInput input,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await AcquireCatalogMutationLockAsync(cancellationToken);
        var resolvedActor = await ResolveActorAsync(actor, cancellationToken);
        if (resolvedActor is null)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.NotFound);
        }

        var entity = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .FirstOrDefaultAsync(
            x => x.Id == modifierId,
            cancellationToken
        );
        if (entity is null)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.NotFound);
        }

        if (entity.IsArchived)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.Archived);
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM modifier_definitions WHERE id = {modifierId} FOR UPDATE""",
                cancellationToken
            );
        }

        if (entity.CurrentVersion is null)
        {
            return new UpdateGameModifierRepositoryResult(
                UpdateGameModifierRepositoryStatus.VersionBindingMissing);
        }
        if (entity.CurrentVersion.Revision != input.ExpectedRevision)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.Stale);
        }

        var existingConflictIds = await GetCurrentConflictIdsAsync(modifierId, cancellationToken);
        var requestedIds = input.ConflictingModifierIds.Distinct().Order().ToArray();
        var requestedDefinitions = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .Where(x => requestedIds.Contains(x.Id) && !x.IsArchived)
            .ToArrayAsync(cancellationToken);
        if (requestedDefinitions.Length != requestedIds.Length)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.NotFound);
        }
        var archivedExistingDefinitions = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .Where(x => existingConflictIds.Contains(x.Id) && x.IsArchived)
            .ToArrayAsync(cancellationToken);
        var desiredIds = requestedIds.Concat(archivedExistingDefinitions.Select(x => x.Id))
            .Distinct().Order().ToArray();
        var incomingContent = ModifierVersionProjector.ContentOf(input) with
        {
            ConflictingModifierIds = desiredIds
        };
        if (requestedDefinitions.Any(x => x.CurrentVersion is null)
            || archivedExistingDefinitions.Any(x => x.CurrentVersion is null))
        {
            return new UpdateGameModifierRepositoryResult(
                UpdateGameModifierRepositoryStatus.VersionBindingMissing);
        }
        var existingContent = ModifierVersionProjector.ContentOf(entity.CurrentVersion, existingConflictIds);
        if (ModifierVersionProjector.ContentEquals(existingContent, incomingContent))
        {
            return new UpdateGameModifierRepositoryResult(
                UpdateGameModifierRepositoryStatus.Unchanged,
                 MapDefinition(entity.CurrentVersion, existingConflictIds));
        }

        var desiredDefinitions = requestedDefinitions.Concat(archivedExistingDefinitions).ToArray();

        var changedCompatibilityIds = existingConflictIds
            .Except(desiredIds).Concat(desiredIds.Except(existingConflictIds)).Distinct().ToArray();
        var affectedIds = changedCompatibilityIds.Append(modifierId).Distinct().ToArray();
        var lockedIds = await GetContentLockedIdsAsync(affectedIds, cancellationToken);
        if (lockedIds.Contains(modifierId))
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.ContentLocked);
        }

        if (lockedIds.Count > 0)
        {
            return new UpdateGameModifierRepositoryResult(UpdateGameModifierRepositoryStatus.CompatibilityLocked);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var desiredNames = desiredDefinitions.ToDictionary(x => x.Id, x => x.CurrentVersion!.Name);
        var targetVersion = ModifierVersionProjector.CreateVersion(
            entity, entity.CurrentVersion.Revision + 1, incomingContent, resolvedActor,
            ModifierVersionChangeTypeValue.Edited, null, now, desiredNames);
        targetVersion.ChangedFields = ModifierVersionProjector.ChangedFields(
            existingContent, incomingContent);
        _dbContext.ModifierDefinitionVersions.Add(targetVersion);
        ModifierVersionProjector.ApplyCurrentProjection(entity, targetVersion);

        var changes = new List<ModifierCatalogChangedItem>
        {
            new(modifierId, targetVersion.Revision, false)
        };
        var affectedDefinitions = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .Where(x => changedCompatibilityIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);
        if (affectedDefinitions.Any(x => x.CurrentVersion is null))
        {
            return new UpdateGameModifierRepositoryResult(
                UpdateGameModifierRepositoryStatus.VersionBindingMissing);
        }
        var affectedVersionIds = affectedDefinitions.Select(x => x.CurrentVersionId!.Value).ToArray();
        var currentConflictRows = await _dbContext.ModifierDefinitionVersionConflicts.AsNoTracking()
            .Where(x => affectedVersionIds.Contains(x.ModifierVersionId))
            .Select(x => new
            {
                ModifierId = x.ModifierVersion.ModifierId,
                ConflictsWithModifierId = x.ConflictingModifierId
            })
            .ToArrayAsync(cancellationToken);
        var cascadeConflictIds = currentConflictRows
            .SelectMany(x => new[] { x.ModifierId, x.ConflictsWithModifierId })
            .Append(modifierId).Distinct().ToArray();
        var cascadeNames = await _dbContext.ModifierDefinitions.AsNoTracking()
            .Where(x => cascadeConflictIds.Contains(x.Id) && x.CurrentVersionId != null)
            .Select(x => new { x.Id, Name = x.CurrentVersion!.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        cascadeNames[modifierId] = targetVersion.Name;
        foreach (var other in affectedDefinitions.OrderBy(x => x.Id))
        {
            var otherCurrentIds = currentConflictRows
                .Where(x => x.ModifierId == other.Id || x.ConflictsWithModifierId == other.Id)
                .Select(x => x.ModifierId == other.Id ? x.ConflictsWithModifierId : x.ModifierId)
                .Distinct().Order().ToArray();
            var nextIds = desiredIds.Contains(other.Id)
                ? otherCurrentIds.Append(modifierId).Distinct().Order().ToArray()
                : otherCurrentIds.Where(x => x != modifierId).Order().ToArray();
            var cascade = CreateVersionForCurrentContent(
                other, nextIds, resolvedActor, ModifierVersionChangeTypeValue.CompatibilityCascade,
                modifierId, null, now, cascadeNames);
            changes.Add(new(other.Id, cascade.Revision, other.IsArchived));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new UpdateGameModifierRepositoryResult(
            UpdateGameModifierRepositoryStatus.Updated,
            MapDefinition(targetVersion, desiredIds),
            changes
        );
    }

    public async Task<ArchiveGameModifierRepositoryStatus> ArchiveModifierAsync(
        Guid modifierId,
        int expectedRevision,
        ModifierChangeActor actor,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await AcquireCatalogMutationLockAsync(cancellationToken);
        var resolvedActor = await ResolveActorAsync(actor, cancellationToken);
        if (resolvedActor is null)
        {
            return ArchiveGameModifierRepositoryStatus.NotFound;
        }
        var entity = await _dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .FirstOrDefaultAsync(
            x => x.Id == modifierId,
            cancellationToken
        );
        if (entity is null || entity.IsArchived)
        {
            return ArchiveGameModifierRepositoryStatus.NotFound;
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM modifier_definitions WHERE id = {modifierId} FOR UPDATE""",
                cancellationToken
            );
        }

        if (await IsContentLockedAsync(modifierId, cancellationToken))
        {
            return ArchiveGameModifierRepositoryStatus.ContentLocked;
        }
        if (entity.CurrentVersion is null)
        {
            return ArchiveGameModifierRepositoryStatus.VersionBindingMissing;
        }
        if (entity.CurrentVersion.Revision != expectedRevision)
        {
            return ArchiveGameModifierRepositoryStatus.Stale;
        }

        entity.IsArchived = true;
        entity.ArchivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        entity.ArchivedByUserId = resolvedActor.UserId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return ArchiveGameModifierRepositoryStatus.Archived;
    }

    public async Task<EmergencyDisableGameModifierRepositoryResult> EmergencyDisableModifierAsync(
        EmergencyDisableGameModifierInput input,
        CancellationToken cancellationToken = default
    )
    {
        var useTransaction = _dbContext.Database.IsRelational();
        await using var transaction = useTransaction
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var activeGameId = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return new EmergencyDisableGameModifierRepositoryResult(
                EmergencyDisableGameModifierRepositoryStatus.GameNotActive
            );
        }

        if (useTransaction)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM games WHERE id = {activeGameId.Value} FOR UPDATE""",
                cancellationToken
            );
        }

        if (!await _dbContext.Games.AsNoTracking().AnyAsync(
                x =>
                    x.Id == activeGameId.Value
                    && x.Status == GameStatusValue.Active
                    && !x.IsDeleted,
                cancellationToken
            ))
        {
            return new EmergencyDisableGameModifierRepositoryResult(
                EmergencyDisableGameModifierRepositoryStatus.GameNotActive
            );
        }

        var enabledModifier = await _dbContext.GameEnabledModifiers.FirstOrDefaultAsync(
            x => x.GameId == activeGameId.Value && x.ModifierId == input.ModifierId,
            cancellationToken
        );
        if (enabledModifier is null)
        {
            return new EmergencyDisableGameModifierRepositoryResult(
                EmergencyDisableGameModifierRepositoryStatus.ModifierNotEnabled
            );
        }

        if (enabledModifier.EmergencyDisabledAtUtc.HasValue)
        {
            return new EmergencyDisableGameModifierRepositoryResult(
                EmergencyDisableGameModifierRepositoryStatus.AlreadyDisabled
            );
        }

        enabledModifier.EmergencyDisabledAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        enabledModifier.EmergencyDisabledByUserId = input.DisabledByUserId;
        enabledModifier.EmergencyDisableReason = input.Reason;

        var board = await _dbContext.GameBoards.FirstAsync(
            x => x.GameId == activeGameId.Value,
            cancellationToken
        );
        board.Version += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new EmergencyDisableGameModifierRepositoryResult(
            EmergencyDisableGameModifierRepositoryStatus.Disabled,
            activeGameId.Value.ToString(),
            board.Version,
            input.ModifierId
        );
    }

    private ModifierDefinitionVersion CreateVersionForCurrentContent(
        ModifierDefinition definition,
        IReadOnlyList<Guid> conflictIds,
        ModifierChangeActor actor,
        string changeType,
        Guid? cascadeSourceModifierId,
        string? note,
        DateTime now,
        Dictionary<Guid, string> allNames)
    {
        var conflictNames = conflictIds.ToDictionary(x => x, x => allNames[x]);
        var current = definition.CurrentVersion
            ?? throw new InvalidOperationException("Current modifier version must be loaded.");
        var content = ModifierVersionProjector.ContentOf(current, conflictIds, note);
        var version = ModifierVersionProjector.CreateVersion(
            definition, current.Revision + 1, content, actor, changeType,
            cascadeSourceModifierId, now, conflictNames);
        _dbContext.ModifierDefinitionVersions.Add(version);
        ModifierVersionProjector.ApplyCurrentProjection(definition, version);
        return version;
    }

}
