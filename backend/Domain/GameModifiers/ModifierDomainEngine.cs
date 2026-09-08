namespace backend.Domain.GameModifiers;

public static class ModifierFormulaCodes
{
    public const string GrowingKillValue = "growing_kill_value";
    public const string BonusKillOnCondition = "bonus_kill_on_condition";
    public const string BonusKillsByCount = "bonus_kills_by_count";
    public const string WindowKillBonusPoints = "window_kill_bonus_points";
    public const string FixedPointsPerUnit = "fixed_points_per_unit";
    public const string CardPercentPerUnit = "card_percent_per_unit";
    public const string BonusKillsPerUnit = "bonus_kills_per_unit";
    public const string KillValueIncreasePerUnit = "kill_value_increase_per_unit";
    public const int Version1 = 1;
}

public sealed record ModifierFormulaDescriptor(
    string Code,
    int Version,
    Type ParameterType,
    IReadOnlySet<Type> ResolutionTypes,
    ModifierRewardKind Reward
);

public static class ModifierFormulaRegistry
{
    private static readonly Dictionary<(string Code, int Version), ModifierFormulaDescriptor>
        Formulas = new Dictionary<(string, int), ModifierFormulaDescriptor>
        {
            [(ModifierFormulaCodes.GrowingKillValue, 1)] = new(
                ModifierFormulaCodes.GrowingKillValue,
                1,
                typeof(GrowingKillValueParameters),
                ResolutionTypes(typeof(AutomaticRoundMetricResolution)),
                ModifierRewardKind.Points
            ),
            [(ModifierFormulaCodes.BonusKillOnCondition, 1)] = new(
                ModifierFormulaCodes.BonusKillOnCondition,
                1,
                typeof(BonusKillOnConditionParameters),
                ResolutionTypes(typeof(BooleanResolution)),
                ModifierRewardKind.BonusKills
            ),
            [(ModifierFormulaCodes.BonusKillsByCount, 1)] = new(
                ModifierFormulaCodes.BonusKillsByCount,
                1,
                typeof(BonusKillsByCountParameters),
                ResolutionTypes(typeof(NonNegativeCountResolution)),
                ModifierRewardKind.BonusKills
            ),
            [(ModifierFormulaCodes.WindowKillBonusPoints, 1)] = new(
                ModifierFormulaCodes.WindowKillBonusPoints,
                1,
                typeof(WindowKillBonusPointsParameters),
                ResolutionTypes(typeof(NonNegativeCountResolution)),
                ModifierRewardKind.Points
            ),
            [(ModifierFormulaCodes.FixedPointsPerUnit, 1)] = new(
                ModifierFormulaCodes.FixedPointsPerUnit,
                1,
                typeof(FixedPointsPerUnitParameters),
                ScoringResolutionTypes(),
                ModifierRewardKind.Points
            ),
            [(ModifierFormulaCodes.CardPercentPerUnit, 1)] = new(
                ModifierFormulaCodes.CardPercentPerUnit,
                1,
                typeof(CardPercentPerUnitParameters),
                ScoringResolutionTypes(),
                ModifierRewardKind.Points
            ),
            [(ModifierFormulaCodes.BonusKillsPerUnit, 1)] = new(
                ModifierFormulaCodes.BonusKillsPerUnit,
                1,
                typeof(BonusKillsPerUnitParameters),
                ScoringResolutionTypes(),
                ModifierRewardKind.BonusKills
            ),
            [(ModifierFormulaCodes.KillValueIncreasePerUnit, 1)] = new(
                ModifierFormulaCodes.KillValueIncreasePerUnit,
                1,
                typeof(KillValueIncreasePerUnitParameters),
                ScoringResolutionTypes(),
                ModifierRewardKind.Points
            )
        };

    private static HashSet<Type> ResolutionTypes(params Type[] values) =>
        new HashSet<Type>(values);

    private static HashSet<Type> ScoringResolutionTypes() => ResolutionTypes(
        typeof(BooleanResolution),
        typeof(NonNegativeCountResolution),
        typeof(AutomaticRoundMetricResolution),
        typeof(PerActivationResolution)
    );

    public static IReadOnlyList<ModifierFormulaDescriptor> All { get; } = Formulas.Values.ToArray();

    public static bool TryGet(
        string code,
        int version,
        out ModifierFormulaDescriptor descriptor
    ) => Formulas.TryGetValue((code, version), out descriptor!);
}

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

public static class ModifierBehaviorValidator
{
    public static string? Validate(ModifierBehaviorV2 behavior)
    {
        if (behavior.SchemaVersion != ModifierBehaviorSchemaVersions.V2
            || string.IsNullOrWhiteSpace(behavior.Rule)
            || behavior.DurationSecondsPerActivation is <= 0)
        {
            return "behavior.invalid";
        }

        if (behavior.Kind == ModifierBehaviorKind.Rule)
        {
            return behavior.Resolution is RuleStatusResolution
                && behavior.Reward == ModifierRewardKind.None
                && behavior.FormulaReference is null
                ? null
                : "behavior.rule_incompatible";
        }

        var formula = behavior.FormulaReference;
        if (formula is null
            || !ModifierFormulaRegistry.TryGet(formula.Code, formula.Version, out var descriptor))
        {
            return "formula.unsupported";
        }

        if (formula.Parameters.GetType() != descriptor.ParameterType
            || !descriptor.ResolutionTypes.Contains(behavior.Resolution.GetType())
            || behavior.Reward != descriptor.Reward)
        {
            return "formula.incompatible";
        }

        if (!IsResolutionConfigurationValid(behavior.Resolution))
        {
            return "resolution.invalid";
        }

        if (RequiresManualInputLabel(formula.Code, behavior.Resolution))
        {
            return "resolution.invalid";
        }

        return formula.Code switch
        {
            ModifierFormulaCodes.GrowingKillValue
                when behavior.Resolution is AutomaticRoundMetricResolution { Metric: "killsCount" }
                    && behavior.Reward == ModifierRewardKind.Points
                    && formula.Parameters is GrowingKillValueParameters p
                    && p.IncrementPointsPerKill >= 0
                    && p.ZeroKillPenaltyPoints >= 0 => null,
            ModifierFormulaCodes.BonusKillOnCondition
                when behavior.Resolution is BooleanResolution
                    && behavior.Reward == ModifierRewardKind.BonusKills
                    && formula.Parameters is BonusKillOnConditionParameters p
                    && p.SuccessBonusKills >= 1 => null,
            ModifierFormulaCodes.BonusKillsByCount
                when behavior.Resolution is NonNegativeCountResolution
                    && behavior.Reward == ModifierRewardKind.BonusKills
                    && formula.Parameters is BonusKillsByCountParameters p
                    && p.BonusKillsPerUnit >= 1 => null,
            ModifierFormulaCodes.WindowKillBonusPoints
                when behavior.Resolution is NonNegativeCountResolution
                    && behavior.Reward == ModifierRewardKind.Points
                    && formula.Parameters is WindowKillBonusPointsParameters p
                    && p.BonusRate > 0 => null,
            ModifierFormulaCodes.FixedPointsPerUnit
                when formula.Parameters is FixedPointsPerUnitParameters p
                    && p.PointsPerUnit != 0 => null,
            ModifierFormulaCodes.CardPercentPerUnit
                when formula.Parameters is CardPercentPerUnitParameters p
                    && p.Rate != 0 => null,
            ModifierFormulaCodes.BonusKillsPerUnit
                when formula.Parameters is BonusKillsPerUnitParameters p
                    && p.BonusKillsPerUnit >= 1 => null,
            ModifierFormulaCodes.KillValueIncreasePerUnit
                when formula.Parameters is KillValueIncreasePerUnitParameters p
                    && p.IncrementPointsPerUnit >= 1
                    && p.ZeroCountPenaltyPoints >= 0 => null,
            _ => "formula.incompatible"
        };
    }

    private static bool IsResolutionConfigurationValid(ModifierResolution resolution) => resolution switch
    {
        RuleStatusResolution => true,
        BooleanResolution value => IsOptionalLabelValid(value.InputLabel),
        AutomaticRoundMetricResolution { Metric: "killsCount" or "bountyCount" } => true,
        PerActivationResolution => true,
        NonNegativeCountResolution value =>
            IsOptionalLabelValid(value.InputLabel)
            && (value.MaximumKind is null or ModifierCountMaximumKinds.None
                || value.MaximumKind == ModifierCountMaximumKinds.ResolvedKills
                || value.MaximumKind == ModifierCountMaximumKinds.Activations)
            && (value.MaximumKind == ModifierCountMaximumKinds.Activations
                ? value.MaximumPerActivation is >= 1
                : value.MaximumPerActivation is null),
        _ => false
    };

    private static bool IsOptionalLabelValid(string? value) =>
        value is null || (!string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 128);

    private static bool RequiresManualInputLabel(string formulaCode, ModifierResolution resolution) =>
        (formulaCode is ModifierFormulaCodes.FixedPointsPerUnit
            or ModifierFormulaCodes.CardPercentPerUnit
            or ModifierFormulaCodes.BonusKillsPerUnit
            or ModifierFormulaCodes.KillValueIncreasePerUnit)
        && resolution switch
        {
            BooleanResolution value => string.IsNullOrWhiteSpace(value.InputLabel),
            NonNegativeCountResolution value => string.IsNullOrWhiteSpace(value.InputLabel),
            _ => false
        };
}

public static class ModifierDomainEngine
{
    public static ModifierEngineResult Calculate(
        ModifierRoundFacts facts,
        IReadOnlyList<ModifierInstanceCalculationInput> instances
    )
    {
        if (facts.CardValue < 0 || facts.KillsCount < 0 || facts.BountyCount < 0)
        {
            return Failure("round_facts.invalid");
        }


        var duplicateActivationIds = instances
            .GroupBy(x => x.Activation.ActivationId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (duplicateActivationIds.Length > 0)
        {
            return new ModifierEngineResult(
                null,
                duplicateActivationIds
                    .Select(x => new ModifierEngineError("activation.duplicate", x))
                    .ToArray()
            );
        }

        var errors = instances
            .Select(x => (Input: x, Error: ModifierBehaviorValidator.Validate(x.Activation.Behavior)))
            .Where(x => x.Error is not null)
            .Select(x => new ModifierEngineError(x.Error!, x.Input.Activation.ActivationId))
            .ToArray();
        if (errors.Length > 0)
        {
            return new ModifierEngineResult(null, errors);
        }

        var outcomes = new List<ModifierInstanceOutcome>(instances.Count);
        foreach (var instance in instances.Where(IsBonusKillFormula))
        {
            var outcome = Resolve(instance, facts, resolvedBonusKills: 0);
            if (outcome.Error is not null)
            {
                return Failure(outcome.Error, instance.Activation.ActivationId);
            }
            outcomes.Add(outcome.Outcome!);
        }

        var resolvedBonusKills = Saturate(outcomes.Sum(x => (long)x.BonusKillsDelta));
        foreach (var instance in instances.Where(x => !IsBonusKillFormula(x)))
        {
            var outcome = Resolve(instance, facts, resolvedBonusKills);
            if (outcome.Error is not null)
            {
                return Failure(outcome.Error, instance.Activation.ActivationId);
            }
            outcomes.Add(outcome.Outcome!);
        }

        var orderedOutcomes = instances
            .Select(x => outcomes.Single(y => y.ActivationId == x.Activation.ActivationId))
            .ToArray();
        var pointsDelta = Saturate(orderedOutcomes.Sum(x => (long)x.PointsDelta));
        var bonusKillsDelta = Saturate(orderedOutcomes.Sum(x => (long)x.BonusKillsDelta));
        var outcomeUnits = Saturate(
            (long)facts.KillsCount + facts.BountyCount + bonusKillsDelta
        );
        var rawCardOutcomeScore = (decimal)facts.CardValue * outcomeUnits;
        var emptyPenaltyApplied = outcomeUnits == 0 && pointsDelta <= 0 && facts.CardValue > 0;
        var emptyPenalty = emptyPenaltyApplied ? -facts.CardValue : 0;
        var finalScore = Saturate(rawCardOutcomeScore + pointsDelta + emptyPenalty);

        return new ModifierEngineResult(
            new ModifierRoundCalculation(
                orderedOutcomes,
                pointsDelta,
                bonusKillsDelta,
                outcomeUnits,
                Saturate(rawCardOutcomeScore),
                emptyPenaltyApplied,
                emptyPenalty,
                finalScore
            ),
            []
        );
    }

    private static bool IsBonusKillFormula(ModifierInstanceCalculationInput input) =>
        input.Activation.Behavior.Reward == ModifierRewardKind.BonusKills;

    private static (ModifierInstanceOutcome? Outcome, string? Error) Resolve(
        ModifierInstanceCalculationInput instance,
        ModifierRoundFacts facts,
        int resolvedBonusKills
    )
    {
        var behavior = instance.Activation.Behavior;
        if (behavior.Kind == ModifierBehaviorKind.Rule)
        {
            return instance.Input is RuleStatusInput rule
                ? (Outcome(instance, ruleOutcome: rule.Outcome), null)
                : (null, "resolution.rule_status_required");
        }

        var formula = behavior.FormulaReference!;
        return formula.Code switch
        {
            ModifierFormulaCodes.GrowingKillValue => ResolveGrowing(instance, facts, formula),
            ModifierFormulaCodes.BonusKillOnCondition => ResolveBoolean(instance, formula),
            ModifierFormulaCodes.BonusKillsByCount => ResolveCountBonus(instance, formula),
            ModifierFormulaCodes.WindowKillBonusPoints => ResolveWindowBonus(
                instance,
                facts,
                resolvedBonusKills,
                formula
            ),
            ModifierFormulaCodes.FixedPointsPerUnit => ResolveGeneric(
                instance,
                facts,
                resolvedBonusKills,
                formula
            ),
            ModifierFormulaCodes.CardPercentPerUnit => ResolveGeneric(
                instance,
                facts,
                resolvedBonusKills,
                formula
            ),
            ModifierFormulaCodes.BonusKillsPerUnit => ResolveGeneric(
                instance,
                facts,
                resolvedBonusKills,
                formula
            ),
            ModifierFormulaCodes.KillValueIncreasePerUnit => ResolveGeneric(
                instance,
                facts,
                resolvedBonusKills,
                formula
            ),
            _ => (null, "formula.unsupported")
        };
    }

    private static (ModifierInstanceOutcome?, string?) ResolveGeneric(
        ModifierInstanceCalculationInput input,
        ModifierRoundFacts facts,
        int resolvedBonusKills,
        ModifierFormulaReference formula
    )
    {
        var unit = ResolveUnit(input, facts, resolvedBonusKills);
        if (unit.Error is not null)
        {
            return (null, unit.Error);
        }

        var quantity = unit.Quantity;
        return formula.Parameters switch
        {
            FixedPointsPerUnitParameters parameters => (
                Outcome(
                    input,
                    pointsDelta: Saturate((long)quantity * parameters.PointsPerUnit),
                    countInput: unit.CountInput,
                    booleanInput: unit.BooleanInput
                ),
                null
            ),
            CardPercentPerUnitParameters parameters => (
                Outcome(
                    input,
                    pointsDelta: SaturatingRoundedProduct(
                        quantity,
                        facts.CardValue,
                        parameters.Rate
                    ),
                    countInput: unit.CountInput,
                    booleanInput: unit.BooleanInput
                ),
                null
            ),
            BonusKillsPerUnitParameters parameters => (
                Outcome(
                    input,
                    bonusKillsDelta: Saturate((long)quantity * parameters.BonusKillsPerUnit),
                    countInput: unit.CountInput,
                    booleanInput: unit.BooleanInput
                ),
                null
            ),
            KillValueIncreasePerUnitParameters parameters => (
                Outcome(
                    input,
                    pointsDelta: quantity == 0
                        ? -parameters.ZeroCountPenaltyPoints
                        : Saturate((long)quantity * parameters.IncrementPointsPerUnit * facts.KillsCount),
                    countInput: unit.CountInput,
                    booleanInput: unit.BooleanInput
                ),
                null
            ),
            _ => (null, "formula.incompatible")
        };
    }

    private static (int Quantity, int? CountInput, bool? BooleanInput, string? Error) ResolveUnit(
        ModifierInstanceCalculationInput input,
        ModifierRoundFacts facts,
        int resolvedBonusKills
    )
    {
        switch (input.Activation.Behavior.Resolution)
        {
            case BooleanResolution when input.Input is BooleanInput value:
                return (value.Succeeded ? 1 : 0, null, value.Succeeded, null);
            case NonNegativeCountResolution resolution when input.Input is NonNegativeCountInput value:
                if (value.Count < 0)
                {
                    return (0, null, null, "resolution.non_negative_count_required");
                }
                if (resolution.MaximumKind == ModifierCountMaximumKinds.ResolvedKills
                    && value.Count > Saturate((long)facts.KillsCount + resolvedBonusKills))
                {
                    return (0, null, null, "resolution.count_exceeds_resolved_kills");
                }
                if (resolution.MaximumKind == ModifierCountMaximumKinds.Activations
                    && value.Count > resolution.MaximumPerActivation)
                {
                    return (0, null, null, "resolution.count_exceeds_activation_limit");
                }
                return (value.Count, value.Count, null, null);
            case AutomaticRoundMetricResolution { Metric: "killsCount" }
                when input.Input is AutomaticRoundMetricInput:
                return (facts.KillsCount, null, null, null);
            case AutomaticRoundMetricResolution { Metric: "bountyCount" }
                when input.Input is AutomaticRoundMetricInput:
                return (facts.BountyCount, null, null, null);
            case PerActivationResolution when input.Input is PerActivationInput:
                return (1, null, null, null);
            case BooleanResolution:
                return (0, null, null, "resolution.boolean_required");
            case NonNegativeCountResolution:
                return (0, null, null, "resolution.non_negative_count_required");
            case AutomaticRoundMetricResolution:
                return (0, null, null, "resolution.automatic_required");
            case PerActivationResolution:
                return (0, null, null, "resolution.per_activation_required");
            default:
                return (0, null, null, "resolution.unsupported");
        }
    }

    private static (ModifierInstanceOutcome?, string?) ResolveGrowing(
        ModifierInstanceCalculationInput input,
        ModifierRoundFacts facts,
        ModifierFormulaReference formula
    )
    {
        if (input.Input is not AutomaticRoundMetricInput)
        {
            return (null, "resolution.automatic_required");
        }

        var parameters = (GrowingKillValueParameters)formula.Parameters;
        var points = facts.KillsCount == 0
            ? -1m * parameters.ZeroKillPenaltyPoints
            : (decimal)parameters.IncrementPointsPerKill * facts.KillsCount * facts.KillsCount;
        return (Outcome(input, pointsDelta: Saturate(points)), null);
    }

    private static (ModifierInstanceOutcome?, string?) ResolveBoolean(
        ModifierInstanceCalculationInput input,
        ModifierFormulaReference formula
    )
    {
        if (input.Input is not BooleanInput value)
        {
            return (null, "resolution.boolean_required");
        }
        var parameters = (BonusKillOnConditionParameters)formula.Parameters;
        return (
            Outcome(
                input,
                bonusKillsDelta: value.Succeeded ? parameters.SuccessBonusKills : 0,
                booleanInput: value.Succeeded
            ),
            null
        );
    }

    private static (ModifierInstanceOutcome?, string?) ResolveCountBonus(
        ModifierInstanceCalculationInput input,
        ModifierFormulaReference formula
    )
    {
        if (input.Input is not NonNegativeCountInput value || value.Count < 0)
        {
            return (null, "resolution.non_negative_count_required");
        }
        var parameters = (BonusKillsByCountParameters)formula.Parameters;
        return (
            Outcome(
                input,
                bonusKillsDelta: Saturate((long)value.Count * parameters.BonusKillsPerUnit),
                countInput: value.Count
            ),
            null
        );
    }

    private static (ModifierInstanceOutcome?, string?) ResolveWindowBonus(
        ModifierInstanceCalculationInput input,
        ModifierRoundFacts facts,
        int resolvedBonusKills,
        ModifierFormulaReference formula
    )
    {
        if (input.Input is not NonNegativeCountInput value || value.Count < 0)
        {
            return (null, "resolution.non_negative_count_required");
        }
        if (value.Count > Saturate((long)facts.KillsCount + resolvedBonusKills))
        {
            return (null, "resolution.count_exceeds_resolved_kills");
        }
        var parameters = (WindowKillBonusPointsParameters)formula.Parameters;
        return (
            Outcome(
                input,
                pointsDelta: SaturatingRoundedProduct(
                    value.Count,
                    facts.CardValue,
                    parameters.BonusRate
                ),
                countInput: value.Count
            ),
            null
        );
    }

    private static ModifierInstanceOutcome Outcome(
        ModifierInstanceCalculationInput input,
        int pointsDelta = 0,
        int bonusKillsDelta = 0,
        ModifierRuleOutcome? ruleOutcome = null,
        int? countInput = null,
        bool? booleanInput = null
    ) => new(
        input.Activation.ActivationId,
        pointsDelta,
        bonusKillsDelta,
        ruleOutcome,
        countInput,
        booleanInput
    );

    private static ModifierEngineResult Failure(string code, Guid? activationId = null) =>
        new(null, [new ModifierEngineError(code, activationId)]);

    private static int Saturate(long value) => value switch
    {
        > int.MaxValue => int.MaxValue,
        < int.MinValue => int.MinValue,
        _ => (int)value
    };

    private static int Saturate(decimal value) => value switch
    {
        > int.MaxValue => int.MaxValue,
        < int.MinValue => int.MinValue,
        _ => decimal.ToInt32(value)
    };

    private static int SaturatingRoundedProduct(int quantity, int cardValue, decimal rate)
    {
        try
        {
            return Saturate(decimal.Round(
                (decimal)quantity * cardValue * rate,
                0,
                MidpointRounding.AwayFromZero
            ));
        }
        catch (OverflowException)
        {
            return rate > 0 ? int.MaxValue : int.MinValue;
        }
    }
}
