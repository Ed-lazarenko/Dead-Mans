using backend.Application.Contracts;
using backend.Data;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

internal sealed class ModifierCatalogReadProjection
{
    private readonly ApplicationDbContext _dbContext;

    public ModifierCatalogReadProjection(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GameModifierDefinition>> LoadAsync(
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.ModifierDefinitions
            .AsNoTracking()
            .Where(x => !x.IsArchived && x.CurrentVersionId != null)
            .Select(x => new { ModifierId = x.Id, Version = x.CurrentVersion! })
            .OrderBy(x => x.Version.ActivationCost)
            .ThenBy(x => x.Version.Name)
            .ToArrayAsync(cancellationToken);

        var modifierIds = rows.Select(x => x.ModifierId).ToArray();
        var versionIds = rows.Select(x => x.Version.Id).ToArray();
        var modifierIdByVersionId = rows.ToDictionary(x => x.Version.Id, x => x.ModifierId);

        var lockedModifierIds = (await _dbContext.GameEnabledModifiers
                .AsNoTracking()
                .Where(x => modifierIds.Contains(x.ModifierId)
                    && x.Game.Status == GameStatusValue.Active
                    && !x.Game.IsDeleted)
                .Select(x => x.ModifierId)
                .Distinct()
                .ToArrayAsync(cancellationToken))
            .ToHashSet();

        var conflictRows = await _dbContext.ModifierDefinitionVersionConflicts
            .AsNoTracking()
            .Where(x => versionIds.Contains(x.ModifierVersionId)
                && modifierIds.Contains(x.ConflictingModifierId))
            .Select(x => new { x.ModifierVersionId, x.ConflictingModifierId })
            .ToArrayAsync(cancellationToken);
        var conflictsByModifierId = conflictRows
            .GroupBy(x => modifierIdByVersionId[x.ModifierVersionId])
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)group.Select(x => x.ConflictingModifierId)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray());

        return rows.Select(row => new GameModifierDefinition(
                row.ModifierId,
                row.Version.Category,
                row.Version.Name,
                row.Version.Description,
                row.Version.ActivationCost,
                new GameModifierActivationLimit(row.Version.MaxActivationsPerRound),
                conflictsByModifierId.GetValueOrDefault(row.ModifierId) ?? [],
                row.Version.IconEmoji,
                row.Version.ActivationCommand,
                lockedModifierIds.Contains(row.ModifierId),
                row.Version.Revision,
                row.Version.NormalizedTags,
                ModifierBehaviorV2Json.Deserialize(row.Version.BehaviorV2Json)))
            .ToArray();
    }
}
