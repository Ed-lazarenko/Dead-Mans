using backend.Domain.Persistence;

namespace backend.Data.Entities;

public class GameRoundModifierResult
{
    public Guid Id { get; set; }

    public Guid RoundId { get; set; }

    public Guid GameModifierActivationId { get; set; }

    public Guid ModifierId { get; set; }

    public string ModifierNameSnapshot { get; set; } = string.Empty;

    public string ModifierCategorySnapshot { get; set; } = string.Empty;

    public string ModifierMechanicTypeSnapshot { get; set; } = string.Empty;

    public string ModifierDescriptionSnapshot { get; set; } = string.Empty;

    public string ModifierScoringTypeSnapshot { get; set; } = string.Empty;

    public string? ModifierEffectSnapshotJson { get; set; }

    public string OutcomeStatus { get; set; } = GameRoundModifierOutcomeValue.Pending;

    public int ScoreDelta { get; set; }

    public int KillDelta { get; set; }

    public decimal? MultiplierApplied { get; set; }

    public string? ResolutionDataJson { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public GameRound Round { get; set; } = default!;

    public GameModifierActivation GameModifierActivation { get; set; } = default!;

    public ModifierDefinition ModifierDefinition { get; set; } = default!;

    public User? ResolvedByUser { get; set; }
}
