namespace backend.Domain.GameModifiers;

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
