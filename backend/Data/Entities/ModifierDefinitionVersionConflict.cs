namespace backend.Data.Entities;

public class ModifierDefinitionVersionConflict
{
    public Guid ModifierVersionId { get; set; }
    public Guid ConflictingModifierId { get; set; }
    public string ConflictingModifierNameSnapshot { get; set; } = string.Empty;

    public ModifierDefinitionVersion ModifierVersion { get; set; } = default!;
    public ModifierDefinition ConflictingModifier { get; set; } = default!;
}
