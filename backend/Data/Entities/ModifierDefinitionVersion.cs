namespace backend.Data.Entities;

public class ModifierDefinitionVersion
{
    public Guid Id { get; set; }
    public Guid ModifierId { get; set; }
    public int Revision { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? IconEmoji { get; set; }
    public string? ActivationCommand { get; set; }
    public int ActivationCost { get; set; }
    public int? MaxActivationsPerRound { get; set; }
    public string[] NormalizedTags { get; set; } = [];
    public string BehaviorV2Json { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string CreatedByDisplayNameSnapshot { get; set; } = string.Empty;
    public string? ChangeNote { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string[] ChangedFields { get; set; } = [];
    public Guid? CascadeSourceModifierId { get; set; }

    public ModifierDefinition Modifier { get; set; } = default!;
    public User? CreatedByUser { get; set; }
    public ModifierDefinition? CascadeSourceModifier { get; set; }
    public ICollection<ModifierDefinitionVersionConflict> Conflicts { get; set; } =
        new List<ModifierDefinitionVersionConflict>();
}
