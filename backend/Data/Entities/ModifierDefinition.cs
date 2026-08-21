namespace backend.Data.Entities;

public class ModifierDefinition
{
    public Guid Id { get; set; }

    public int Revision { get; set; } = 1;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? IconEmoji { get; set; }

    public string? ActivationCommand { get; set; }

    public int ActivationCost { get; set; }

    public int? MaxActivationsPerRound { get; set; }

    public string[] NormalizedTags { get; set; } = [];

    public string BehaviorV2Json { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<GameEnabledModifier> EnabledInGames { get; set; } =
        new List<GameEnabledModifier>();

    public ICollection<GameModifierActivation> GameActivations { get; set; } =
        new List<GameModifierActivation>();
}
