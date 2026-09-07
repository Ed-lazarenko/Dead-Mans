namespace backend.Data.Entities;

public class ModifierDefinition
{
    public Guid Id { get; set; }

    public Guid? CurrentVersionId { get; set; }

    public bool IsArchived { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }

    public Guid? ArchivedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ModifierDefinitionVersion? CurrentVersion { get; set; }

    public User? CreatedByUser { get; set; }

    public User? ArchivedByUser { get; set; }

    public ICollection<ModifierDefinitionVersion> Versions { get; set; } =
        new List<ModifierDefinitionVersion>();

    public ICollection<GameEnabledModifier> EnabledInGames { get; set; } =
        new List<GameEnabledModifier>();

    public ICollection<GameModifierActivation> GameActivations { get; set; } =
        new List<GameModifierActivation>();
}
