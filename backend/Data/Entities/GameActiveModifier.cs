namespace backend.Data.Entities;

public class GameActiveModifier
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid ModifierId { get; set; }

    public Guid ActivatedByUserId { get; set; }

    public int ActivationCostSnapshot { get; set; }

    public DateTime ActivatedAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }

    public Game Game { get; set; } = default!;

    public ModifierDefinition ModifierDefinition { get; set; } = default!;

    public User? ActivatedByUser { get; set; }
}
