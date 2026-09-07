using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Support;

internal sealed record TestModifierSpec(
    Guid Id,
    string Name,
    string Description,
    string Category,
    int ActivationCost,
    int? MaxActivationsPerRound,
    ModifierBehaviorV2 Behavior,
    string[]? NormalizedTags = null,
    string? IconEmoji = null,
    string? ActivationCommand = null,
    int Revision = 1,
    IReadOnlyList<Guid>? ConflictingModifierIds = null);

internal static class TestModifierVersionFactory
{
    public static async Task<IReadOnlyDictionary<Guid, ModifierDefinitionVersion>> AddAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<TestModifierSpec> specs,
        DateTime? createdAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var at = createdAtUtc ?? DateTime.UtcNow;
        var roots = specs.Select(spec => new ModifierDefinition
        {
            Id = spec.Id,
            CreatedAtUtc = at
        }).ToArray();
        dbContext.ModifierDefinitions.AddRange(roots);
        await dbContext.SaveChangesAsync(cancellationToken);

        var localNames = specs.ToDictionary(x => x.Id, x => x.Name);
        var externalConflictIds = specs.SelectMany(x => x.ConflictingModifierIds ?? [])
            .Where(x => !localNames.ContainsKey(x)).Distinct().ToArray();
        var externalNames = externalConflictIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.ModifierDefinitions.AsNoTracking()
                .Where(x => externalConflictIds.Contains(x.Id) && x.CurrentVersionId != null)
                .Select(x => new { x.Id, Name = x.CurrentVersion!.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var versions = specs.Select(spec =>
        {
            var version = new ModifierDefinitionVersion
            {
                Id = Guid.NewGuid(),
                ModifierId = spec.Id,
                Revision = spec.Revision,
                Name = spec.Name,
                Description = spec.Description,
                Category = spec.Category,
                IconEmoji = spec.IconEmoji,
                ActivationCommand = spec.ActivationCommand,
                ActivationCost = spec.ActivationCost,
                MaxActivationsPerRound = spec.MaxActivationsPerRound,
                NormalizedTags = spec.NormalizedTags ?? ["test"],
                BehaviorV2Json = ModifierBehaviorV2Json.Serialize(spec.Behavior),
                CreatedAtUtc = at,
                CreatedByDisplayNameSnapshot = "Test fixture",
                ChangeType = ModifierVersionChangeTypeValue.Created,
                ChangedFields = ["created"]
            };
            foreach (var conflictId in spec.ConflictingModifierIds ?? [])
            {
                version.Conflicts.Add(new ModifierDefinitionVersionConflict
                {
                    ModifierVersionId = version.Id,
                    ConflictingModifierId = conflictId,
                    ConflictingModifierNameSnapshot = localNames.GetValueOrDefault(conflictId)
                        ?? externalNames[conflictId]
                });
            }
            return version;
        }).ToArray();
        dbContext.ModifierDefinitionVersions.AddRange(versions);
        await dbContext.SaveChangesAsync(cancellationToken);

        var versionByModifier = versions.ToDictionary(x => x.ModifierId);
        foreach (var root in roots)
        {
            root.CurrentVersionId = versionByModifier[root.Id].Id;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return versionByModifier;
    }

    public static async Task<ModifierDefinitionVersion> AddAsync(
        ApplicationDbContext dbContext,
        TestModifierSpec spec,
        DateTime? createdAtUtc = null,
        CancellationToken cancellationToken = default) =>
        (await AddAsync(dbContext, [spec], createdAtUtc, cancellationToken))[spec.Id];

    public static async Task<ModifierDefinitionVersion> AddRevisionAsync(
        ApplicationDbContext dbContext,
        Guid modifierId,
        Action<ModifierDefinitionVersion> configure,
        CancellationToken cancellationToken = default)
    {
        var root = await dbContext.ModifierDefinitions
            .Include(x => x.CurrentVersion)
            .ThenInclude(x => x!.Conflicts)
            .SingleAsync(x => x.Id == modifierId, cancellationToken);
        var current = root.CurrentVersion
            ?? throw new InvalidOperationException("Fixture modifier has no current version.");
        var next = new ModifierDefinitionVersion
        {
            Id = Guid.NewGuid(),
            ModifierId = modifierId,
            Revision = current.Revision + 1,
            Name = current.Name,
            Description = current.Description,
            Category = current.Category,
            IconEmoji = current.IconEmoji,
            ActivationCommand = current.ActivationCommand,
            ActivationCost = current.ActivationCost,
            MaxActivationsPerRound = current.MaxActivationsPerRound,
            NormalizedTags = current.NormalizedTags.ToArray(),
            BehaviorV2Json = current.BehaviorV2Json,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByDisplayNameSnapshot = "Test fixture",
            ChangeType = ModifierVersionChangeTypeValue.Edited,
            ChangedFields = ["test"]
        };
        foreach (var conflict in current.Conflicts)
        {
            next.Conflicts.Add(new ModifierDefinitionVersionConflict
            {
                ModifierVersionId = next.Id,
                ConflictingModifierId = conflict.ConflictingModifierId,
                ConflictingModifierNameSnapshot = conflict.ConflictingModifierNameSnapshot
            });
        }
        configure(next);
        dbContext.ModifierDefinitionVersions.Add(next);
        await dbContext.SaveChangesAsync(cancellationToken);
        root.CurrentVersionId = next.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        return next;
    }
}
