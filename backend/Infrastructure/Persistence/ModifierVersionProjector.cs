using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;

namespace backend.Infrastructure.Persistence;

internal static class ModifierVersionProjector
{
    public static ModifierDefinitionVersion CreateVersion(
        ModifierDefinition definition,
        int revision,
        CreateGameModifierInput content,
        ModifierChangeActor? actor,
        string changeType,
        Guid? cascadeSourceModifierId,
        DateTime now,
        IReadOnlyDictionary<Guid, string> conflictNames)
    {
        var version = new ModifierDefinitionVersion
        {
            Id = Guid.NewGuid(),
            ModifierId = definition.Id,
            Revision = revision,
            Name = content.Name,
            Description = content.Description,
            Category = content.Category,
            IconEmoji = content.IconEmoji,
            ActivationCommand = content.ActivationCommand,
            ActivationCost = content.ActivationCost,
            MaxActivationsPerRound = content.ActivationLimit.Count,
            NormalizedTags = (content.NormalizedTags ?? []).ToArray(),
            BehaviorV2Json = ModifierBehaviorV2Json.Serialize(content.BehaviorV2),
            CreatedAtUtc = now,
            CreatedByUserId = actor?.UserId,
            CreatedByDisplayNameSnapshot = actor?.DisplayName ?? "System migration",
            ChangeNote = content.ChangeNote,
            ChangeType = changeType,
            ChangedFields = changeType switch
            {
                ModifierVersionChangeTypeValue.Created => ["created"],
                ModifierVersionChangeTypeValue.MigrationBaseline => ["created"],
                ModifierVersionChangeTypeValue.CompatibilityCascade => ["compatibility"],
                _ => []
            },
            CascadeSourceModifierId = cascadeSourceModifierId
        };
        foreach (var pair in conflictNames.OrderBy(x => x.Key))
        {
            version.Conflicts.Add(new ModifierDefinitionVersionConflict
            {
                ModifierVersionId = version.Id,
                ConflictingModifierId = pair.Key,
                ConflictingModifierNameSnapshot = pair.Value
            });
        }
        return version;
    }

    public static CreateGameModifierInput ContentOf(
        ModifierDefinitionVersion version,
        IReadOnlyList<Guid> conflicts,
        string? changeNote = null) => new(
            version.Name, version.Description, version.Category,
            version.ActivationCost, new GameModifierActivationLimit(version.MaxActivationsPerRound),
            conflicts, version.IconEmoji, version.ActivationCommand,
            version.NormalizedTags, ModifierBehaviorV2Json.Deserialize(version.BehaviorV2Json),
            changeNote);

    public static CreateGameModifierInput ContentOf(UpdateGameModifierInput input) => new(
        input.Name, input.Description, input.Category, input.ActivationCost, input.ActivationLimit,
        input.ConflictingModifierIds, input.IconEmoji, input.ActivationCommand,
        input.NormalizedTags, input.BehaviorV2, input.ChangeNote);

    public static void ApplyCurrentProjection(
        ModifierDefinition definition,
        ModifierDefinitionVersion version)
    {
        definition.CurrentVersionId = version.Id;
    }

    public static bool ContentEquals(
        CreateGameModifierInput left,
        CreateGameModifierInput right) =>
        left.Name == right.Name
        && left.Description == right.Description
        && left.Category == right.Category
        && left.IconEmoji == right.IconEmoji
        && left.ActivationCommand == right.ActivationCommand
        && left.ActivationCost == right.ActivationCost
        && left.ActivationLimit.Count == right.ActivationLimit.Count
        && left.NormalizedTags!.SequenceEqual(right.NormalizedTags!)
        && ModifierBehaviorV2Json.Serialize(left.BehaviorV2)
            == ModifierBehaviorV2Json.Serialize(right.BehaviorV2)
        && left.ConflictingModifierIds.Order().SequenceEqual(right.ConflictingModifierIds.Order());

    public static IReadOnlyList<string> ChangedFields(
        ModifierDefinitionVersion? previous,
        IReadOnlyList<Guid> previousConflicts,
        ModifierDefinitionVersion current,
        IReadOnlyList<Guid> currentConflicts)
    {
        if (previous is null)
        {
            return ["created"];
        }
        var fields = new List<string>();
        Add(previous.Name != current.Name, "name");
        Add(previous.Description != current.Description, "description");
        Add(previous.Category != current.Category, "category");
        Add(previous.IconEmoji != current.IconEmoji, "iconEmoji");
        Add(previous.ActivationCommand != current.ActivationCommand, "activationCommand");
        Add(previous.ActivationCost != current.ActivationCost, "activationCost");
        Add(previous.MaxActivationsPerRound != current.MaxActivationsPerRound, "activationLimit");
        Add(!previous.NormalizedTags.SequenceEqual(current.NormalizedTags), "normalizedTags");
        Add(previous.BehaviorV2Json != current.BehaviorV2Json, "behaviorV2");
        Add(!previousConflicts.Order().SequenceEqual(currentConflicts.Order()), "compatibility");
        return fields;

        void Add(bool changed, string name)
        {
            if (changed) fields.Add(name);
        }
    }

    public static string[] ChangedFields(
        CreateGameModifierInput previous,
        CreateGameModifierInput current)
    {
        var fields = new List<string>();
        Add(previous.Name != current.Name, "name");
        Add(previous.Description != current.Description, "description");
        Add(previous.Category != current.Category, "category");
        Add(previous.IconEmoji != current.IconEmoji, "iconEmoji");
        Add(previous.ActivationCommand != current.ActivationCommand, "activationCommand");
        Add(previous.ActivationCost != current.ActivationCost, "activationCost");
        Add(previous.ActivationLimit.Count != current.ActivationLimit.Count, "activationLimit");
        Add(!previous.NormalizedTags!.SequenceEqual(current.NormalizedTags!), "normalizedTags");
        Add(ModifierBehaviorV2Json.Serialize(previous.BehaviorV2)
            != ModifierBehaviorV2Json.Serialize(current.BehaviorV2), "behaviorV2");
        Add(!previous.ConflictingModifierIds.Order().SequenceEqual(
            current.ConflictingModifierIds.Order()), "compatibility");
        return fields.ToArray();

        void Add(bool changed, string name)
        {
            if (changed) fields.Add(name);
        }
    }
}
