using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameModifierRepository
{
    private async Task<ModifierChangeActor?> ResolveActorAsync(
        ModifierChangeActor actor,
        CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
        {
            return actor.UserId != Guid.Empty ? actor : null;
        }
        var name = await _dbContext.Users.AsNoTracking()
            .Where(x => x.Id == actor.UserId && x.IsActive)
            .Select(x => x.DisplayName).SingleOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(name) ? null : new ModifierChangeActor(actor.UserId, name);
    }

    private async Task AcquireCatalogMutationLockAsync(CancellationToken cancellationToken)
    {
        await ModifierCatalogTransactionLock.AcquireAsync(_dbContext, cancellationToken);
    }

    private Task<Guid[]> GetCurrentConflictIdsAsync(Guid modifierId, CancellationToken cancellationToken) =>
        _dbContext.ModifierDefinitionVersionConflicts.AsNoTracking()
            .Where(x => x.ModifierVersion.ModifierId == modifierId
                && x.ModifierVersion.Modifier.CurrentVersionId == x.ModifierVersionId)
            .Select(x => x.ConflictingModifierId)
            .OrderBy(x => x).ToArrayAsync(cancellationToken);

    private async Task<HashSet<Guid>> GetContentLockedIdsAsync(
        IReadOnlyList<Guid> modifierIds,
        CancellationToken cancellationToken) =>
        (await _dbContext.GameEnabledModifiers.AsNoTracking()
            .Where(x => modifierIds.Contains(x.ModifierId)
                && x.Game.Status == GameStatusValue.Active && !x.Game.IsDeleted)
            .Select(x => x.ModifierId).Distinct().ToArrayAsync(cancellationToken)).ToHashSet();

    private async Task<bool> IsAnyContentLockedAsync(
        IReadOnlyList<Guid> modifierIds,
        CancellationToken cancellationToken) =>
        (await GetContentLockedIdsAsync(modifierIds, cancellationToken)).Count > 0;

    private Task<bool> IsContentLockedAsync(Guid modifierId, CancellationToken cancellationToken)
    {
        return _dbContext.GameEnabledModifiers
            .AsNoTracking()
            .AnyAsync(
                x => x.ModifierId == modifierId
                    && x.Game.Status == GameStatusValue.Active
                    && !x.Game.IsDeleted,
                cancellationToken
            );
    }

    private static GameModifierDefinition MapDefinition(
        ModifierDefinitionVersion x,
        IReadOnlyList<Guid> conflictingModifierIds,
        bool isLockedByActiveGame = false)
    {
        return new GameModifierDefinition(
            x.ModifierId, x.Category, x.Name, x.Description, x.ActivationCost,
            new GameModifierActivationLimit(x.MaxActivationsPerRound),
            conflictingModifierIds, x.IconEmoji, x.ActivationCommand,
            isLockedByActiveGame, x.Revision, x.NormalizedTags,
            ResolveBehaviorV2(x));
    }

    private static ModifierBehaviorV2 ResolveBehaviorV2(ModifierDefinitionVersion version)
    {
        return ModifierBehaviorV2Json.Deserialize(version.BehaviorV2Json);
    }
}
