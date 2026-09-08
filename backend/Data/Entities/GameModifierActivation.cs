using backend.Domain.Persistence;

namespace backend.Data.Entities;

public class GameModifierActivation
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid RoundId { get; set; }

    public Guid ModifierId { get; set; }

    public Guid ModifierVersionId { get; set; }

    public Guid ActivatedByUserId { get; set; }

    public Guid InitiatedByUserId { get; set; }

    public int ActivationCostSnapshot { get; set; }

    public int DefinitionRevisionSnapshot { get; set; }

    public string ModifierNameSnapshot { get; set; } = string.Empty;

    public string ModifierDescriptionSnapshot { get; set; } = string.Empty;

    public string ModifierCategorySnapshot { get; set; } = string.Empty;

    public string? ModifierIconEmojiSnapshot { get; set; }

    public string? ActivationCommandSnapshot { get; set; }

    public string[] NormalizedTagsSnapshot { get; set; } = [];

    public string BehaviorV2SnapshotJson { get; set; } = string.Empty;

    public DateTime ActivatedAtUtc { get; set; }

    public string Status { get; set; } = GameModifierActivationStatusValue.Active;

    public DateTime? ArchivedAtUtc { get; set; }

    public Guid? CancelledByUserId { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    public string? CancellationReason { get; set; }

    public int RefundAmount { get; set; }

    public Game Game { get; set; } = default!;

    public GameRound Round { get; set; } = default!;

    public ModifierDefinition ModifierDefinition { get; set; } = default!;

    public ModifierDefinitionVersion? ModifierVersion { get; set; }

    public GameEnabledModifier EnabledModifier { get; set; } = default!;

    public User? ActivatedByUser { get; set; }

    public User? InitiatedByUser { get; set; }

    public User? CancelledByUser { get; set; }
}
