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

    public string ModifierDescriptionSnapshot { get; set; } = string.Empty;

    public int DefinitionRevisionSnapshot { get; set; }

    public string? ModifierActivationCommandSnapshot { get; set; }

    public string[] ModifierNormalizedTagsSnapshot { get; set; } = [];

    public string ModifierBehaviorV2SnapshotJson { get; set; } = string.Empty;

    public string OutcomeStatus { get; set; } = GameRoundModifierOutcomeValue.Pending;

    public int ScoreDelta { get; set; }

    public int KillDelta { get; set; }

    public decimal? MultiplierApplied { get; set; }

    public string? ResolutionDataJson { get; set; }

    public Guid? ResolutionGroupId { get; set; }

    public string? ResolutionKind { get; set; }

    public string? ViolationComment { get; set; }

    public string? CalculationBreakdownJson { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public GameRound Round { get; set; } = default!;

    public GameModifierActivation GameModifierActivation { get; set; } = default!;

    public ModifierDefinition ModifierDefinition { get; set; } = default!;

    public User? ResolvedByUser { get; set; }
}
