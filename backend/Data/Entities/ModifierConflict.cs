namespace backend.Data.Entities;

public class ModifierConflict
{
    public Guid ModifierId { get; set; }

    public Guid ConflictsWithModifierId { get; set; }

    public ModifierDefinition Modifier { get; set; } = default!;

    public ModifierDefinition ConflictsWithModifier { get; set; } = default!;
}
