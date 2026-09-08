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

    public static IReadOnlyList<ModifierFormulaDescriptor> All { get; } = Formulas.Values.ToArray();

    public static bool TryGet(
        string code,
        int version,
        out ModifierFormulaDescriptor descriptor
    ) => Formulas.TryGetValue((code, version), out descriptor!);

    private static HashSet<Type> ResolutionTypes(params Type[] values) =>
        new HashSet<Type>(values);

    private static HashSet<Type> ScoringResolutionTypes() => ResolutionTypes(
        typeof(BooleanResolution),
        typeof(NonNegativeCountResolution),
        typeof(AutomaticRoundMetricResolution),
        typeof(PerActivationResolution)
    );
}
