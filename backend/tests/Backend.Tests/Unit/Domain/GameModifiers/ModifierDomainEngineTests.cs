using backend.Domain.GameModifiers;

namespace Backend.Tests.Unit.Domain.GameModifiers;

public sealed class ModifierDomainEngineTests
{
    [Theory]
    [InlineData(0, -25, -125)]
    [InlineData(1, 5, 105)]
    [InlineData(3, 45, 345)]
    public void GrowingKillValue_UsesApprovedGoldenFormula(
        int kills,
        int expectedPointsDelta,
        int expectedFinalScore
    )
    {
        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, kills, 0),
            [Automatic(ModifierFormulaCodes.GrowingKillValue, new GrowingKillValueParameters(5, 25))]
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedPointsDelta, result.Calculation!.PointsDelta);
        Assert.Equal(expectedFinalScore, result.Calculation.FinalScore);
    }

    [Fact]
    public void GrowingKillValue_MultipleInstances_AreCalculatedIndependentlyAndSummed()
    {
        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 3, 0),
            [
                Automatic(ModifierFormulaCodes.GrowingKillValue, new GrowingKillValueParameters(5, 25)),
                Automatic(ModifierFormulaCodes.GrowingKillValue, new GrowingKillValueParameters(5, 25))
            ]
        );

        Assert.True(result.IsSuccess);
        Assert.Equal([45, 45], result.Calculation!.Instances.Select(x => x.PointsDelta));
        Assert.Equal(90, result.Calculation.PointsDelta);
        Assert.Equal(390, result.Calculation.FinalScore);
    }

    [Theory]
    [InlineData(1, 115, 345)]
    [InlineData(2, 130, 390)]
    public void GrowingKillValue_AddsTheAccumulatedBonusToCardValueBeforeMultiplyingKills(
        int activationCount,
        int expectedValuePerKill,
        int expectedFinalScore
    )
    {
        const int cardValue = 100;
        const int kills = 3;
        var instances = Enumerable.Range(0, activationCount)
            .Select(_ => Automatic(
                ModifierFormulaCodes.GrowingKillValue,
                new GrowingKillValueParameters(5, 25)
            ))
            .ToArray();

        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(cardValue, kills, 0),
            instances
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(cardValue + (5 * kills * activationCount), expectedValuePerKill);
        Assert.Equal(expectedValuePerKill * kills, result.Calculation!.FinalScore);
        Assert.Equal(expectedFinalScore, result.Calculation.FinalScore);
    }

    [Fact]
    public void BonusKillOnCondition_ResolvesEachShotInstanceExactlyOnce()
    {
        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 0, 0),
            [BooleanBonus(true), BooleanBonus(false), BooleanBonus(true)]
        );

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 0, 1], result.Calculation!.Instances.Select(x => x.BonusKillsDelta));
        Assert.Equal(2, result.Calculation.BonusKillsDelta);
        Assert.False(result.Calculation.EmptyCardPenaltyApplied);
        Assert.Equal(200, result.Calculation.FinalScore);
    }

    [Fact]
    public void BonusKillsByCount_AcceptsZeroAndRejectsNegativeCount()
    {
        var zero = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 0, 0),
            [CountBonus(0)]
        );
        var negative = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 0, 0),
            [CountBonus(-1)]
        );

        Assert.True(zero.IsSuccess);
        Assert.Equal(0, zero.Calculation!.BonusKillsDelta);
        Assert.True(zero.Calculation.EmptyCardPenaltyApplied);
        Assert.False(negative.IsSuccess);
        Assert.Equal("resolution.non_negative_count_required", Assert.Single(negative.Errors).Code);
    }

    [Fact]
    public void WindowKillBonusPoints_IsDirectPointsAndRoundsAwayFromZero()
    {
        var regular = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 2, 0),
            [WindowBonus(2, 0.75m)]
        );
        var midpoint = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(1, 1, 0),
            [WindowBonus(1, 0.5m)]
        );

        Assert.True(regular.IsSuccess);
        Assert.Equal(150, regular.Calculation!.PointsDelta);
        Assert.Equal(350, regular.Calculation.FinalScore);
        Assert.Equal(1, midpoint.Calculation!.PointsDelta);
        Assert.Equal(2, midpoint.Calculation.FinalScore);
    }

    [Fact]
    public void WindowKillBonusPoints_ValidatesAgainstKillsPlusResolvedBonusKills()
    {
        var valid = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 1, 0),
            [BooleanBonus(true), WindowBonus(2, 0.75m)]
        );
        var invalid = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 1, 0),
            [BooleanBonus(true), WindowBonus(3, 0.75m)]
        );

        Assert.True(valid.IsSuccess);
        Assert.Equal(150, valid.Calculation!.PointsDelta);
        Assert.False(invalid.IsSuccess);
        Assert.Equal(
            "resolution.count_exceeds_resolved_kills",
            Assert.Single(invalid.Errors).Code
        );
    }

    [Fact]
    public void InvalidFormulaConfiguration_FailsTheWholeCalculationWithoutPartialScore()
    {
        var invalid = Automatic(
            ModifierFormulaCodes.GrowingKillValue,
            new GrowingKillValueParameters(-1, 25)
        );

        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 2, 0),
            [BooleanBonus(true), invalid]
        );

        Assert.False(result.IsSuccess);
        Assert.Null(result.Calculation);
        Assert.Equal("formula.incompatible", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void PublishedAggregates_SaturateToInt32()
    {
        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(int.MaxValue, int.MaxValue, int.MaxValue),
            [
                Automatic(
                    ModifierFormulaCodes.GrowingKillValue,
                    new GrowingKillValueParameters(int.MaxValue, int.MaxValue)
                )
            ]
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(int.MaxValue, result.Calculation!.PointsDelta);
        Assert.Equal(int.MaxValue, result.Calculation.CardOutcomeUnits);
        Assert.Equal(int.MaxValue, result.Calculation.FinalScore);
    }

    [Fact]
    public void RuleBehavior_ProducesTypedOutcomeWithoutNumericalReward()
    {
        var activation = Snapshot(
            new ModifierBehaviorV2(
                ModifierBehaviorSchemaVersions.V2,
                ModifierBehaviorKind.Rule,
                ModifierPhase.Round,
                ModifierPerformer.ActiveTeam,
                true,
                "Do the thing",
                ModifierStackingPolicy.AggregateParameters,
                new RuleStatusResolution(),
                ModifierRewardKind.None,
                null
            )
        );
        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 1, 0),
            [new ModifierInstanceCalculationInput(activation, new RuleStatusInput(ModifierRuleOutcome.Violated))]
        );

        Assert.True(result.IsSuccess);
        var outcome = Assert.Single(result.Calculation!.Instances);
        Assert.Equal(ModifierRuleOutcome.Violated, outcome.RuleOutcome);
        Assert.Equal(0, outcome.PointsDelta);
        Assert.Equal(0, outcome.BonusKillsDelta);
    }

    [Fact]
    public void FormulaRegistry_ContainsExactlyTheFourVersionedBuiltIns()
    {
        Assert.Equal(4, ModifierFormulaRegistry.All.Count);
        Assert.All(ModifierFormulaRegistry.All, formula => Assert.Equal(1, formula.Version));
        Assert.Equal(
            [
                ModifierFormulaCodes.BonusKillOnCondition,
                ModifierFormulaCodes.BonusKillsByCount,
                ModifierFormulaCodes.GrowingKillValue,
                ModifierFormulaCodes.WindowKillBonusPoints
            ],
            ModifierFormulaRegistry.All.Select(x => x.Code).OrderBy(x => x)
        );
    }

    [Fact]
    public void DuplicateActivationId_FailsClosedInsteadOfDoubleCounting()
    {
        var instance = BooleanBonus(true);
        var result = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(100, 0, 0),
            [instance, instance]
        );

        Assert.False(result.IsSuccess);
        Assert.Equal("activation.duplicate", Assert.Single(result.Errors).Code);
    }

    private static ModifierInstanceCalculationInput Automatic(
        string code,
        ModifierFormulaParameters parameters
    ) => Input(
        new AutomaticRoundMetricResolution("killsCount"),
        ModifierRewardKind.Points,
        new ModifierFormulaReference(code, 1, parameters),
        new AutomaticRoundMetricInput()
    );

    private static ModifierInstanceCalculationInput BooleanBonus(bool succeeded) => Input(
        new BooleanResolution(),
        ModifierRewardKind.BonusKills,
        new ModifierFormulaReference(
            ModifierFormulaCodes.BonusKillOnCondition,
            1,
            new BonusKillOnConditionParameters(1)
        ),
        new BooleanInput(succeeded)
    );

    private static ModifierInstanceCalculationInput CountBonus(int count) => Input(
        new NonNegativeCountResolution(),
        ModifierRewardKind.BonusKills,
        new ModifierFormulaReference(
            ModifierFormulaCodes.BonusKillsByCount,
            1,
            new BonusKillsByCountParameters(1)
        ),
        new NonNegativeCountInput(count)
    );

    private static ModifierInstanceCalculationInput WindowBonus(int count, decimal rate) => Input(
        new NonNegativeCountResolution(),
        ModifierRewardKind.Points,
        new ModifierFormulaReference(
            ModifierFormulaCodes.WindowKillBonusPoints,
            1,
            new WindowKillBonusPointsParameters(rate)
        ),
        new NonNegativeCountInput(count)
    );

    private static ModifierInstanceCalculationInput Input(
        ModifierResolution resolution,
        ModifierRewardKind reward,
        ModifierFormulaReference formula,
        ModifierResolutionInput input
    ) => new(
        Snapshot(
            new ModifierBehaviorV2(
                ModifierBehaviorSchemaVersions.V2,
                ModifierBehaviorKind.Scoring,
                ModifierPhase.Result,
                ModifierPerformer.ActiveTeam,
                true,
                "Scoring rule",
                ModifierStackingPolicy.IndependentInstances,
                resolution,
                reward,
                formula
            )
        ),
        input
    );

    private static ModifierActivationSnapshotV2 Snapshot(ModifierBehaviorV2 behavior) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        1,
        "Test modifier",
        behavior
    );
}
