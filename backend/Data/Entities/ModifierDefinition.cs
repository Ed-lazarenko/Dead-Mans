namespace backend.Data.Entities;

public class ModifierDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ScoringType { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool RequiresHostControl { get; set; }

    public string? IconEmoji { get; set; }

    public string? ActivationCommand { get; set; }

    public int ActivationCost { get; set; }

    public int? DefaultLimitPerGame { get; set; }

    public string? MetadataJson { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<GameEnabledModifier> EnabledInGames { get; set; } =
        new List<GameEnabledModifier>();

    public ICollection<GameModifierActivation> GameActivations { get; set; } =
        new List<GameModifierActivation>();
}
