namespace backend.Domain.GameModifiers;

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
