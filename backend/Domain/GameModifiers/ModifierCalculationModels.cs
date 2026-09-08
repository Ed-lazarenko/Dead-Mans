namespace backend.Domain.GameModifiers;

public sealed record ModifierRoundFacts(int CardValue, int KillsCount, int BountyCount);

public sealed record ModifierInstanceCalculationInput(
    ModifierActivationSnapshotV2 Activation,
    ModifierResolutionInput Input
);

public sealed record ModifierInstanceOutcome(
    Guid ActivationId,
    int PointsDelta,
    int BonusKillsDelta,
    ModifierRuleOutcome? RuleOutcome,
    int? CountInput,
    bool? BooleanInput
);

public sealed record ModifierRoundCalculation(
    IReadOnlyList<ModifierInstanceOutcome> Instances,
    int PointsDelta,
    int BonusKillsDelta,
    int CardOutcomeUnits,
    int CardOutcomeScore,
    bool EmptyCardPenaltyApplied,
    int EmptyCardPenalty,
    int FinalScore
);

public sealed record ModifierEngineError(string Code, Guid? ActivationId = null);

public sealed record ModifierEngineResult(
    ModifierRoundCalculation? Calculation,
    IReadOnlyList<ModifierEngineError> Errors
)
{
    public bool IsSuccess => Calculation is not null && Errors.Count == 0;
}
