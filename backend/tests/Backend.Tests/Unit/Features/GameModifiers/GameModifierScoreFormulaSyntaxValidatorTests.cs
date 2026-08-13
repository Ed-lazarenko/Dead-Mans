using backend.Application.Contracts;
using backend.Application.Features.GameModifiers;

namespace Backend.Tests.Unit.Features.GameModifiers;

public sealed class GameModifierScoreFormulaSyntaxValidatorTests
{
    [Fact]
    public void EvaluateSuccess_WhenFlatFormulaUsesLargeValues_DoesNotOverflowIntegerArithmetic()
    {
        var formula = new GameModifierScoreFormula(
            GameModifierScoreFormulaModes.FlatPerKill,
            null,
            null
        );
        var context = CreateContext(int.MaxValue, int.MaxValue);

        var result = GameModifierScoreFormulaSyntaxValidator.EvaluateSuccess(formula, context);

        Assert.Equal((double)int.MaxValue * int.MaxValue, result);
        Assert.True(result > 0);
    }

    [Fact]
    public void EvaluateSuccess_WhenStackingFormulaUsesLargeValues_DoesNotOverflowIntegerArithmetic()
    {
        var formula = new GameModifierScoreFormula(
            GameModifierScoreFormulaModes.StackingPerKillBonus,
            null,
            null
        );
        var context = CreateContext(int.MaxValue, int.MaxValue);

        var result = GameModifierScoreFormulaSyntaxValidator.EvaluateSuccess(formula, context);

        Assert.Equal((double)int.MaxValue * int.MaxValue * int.MaxValue, result);
        Assert.True(result > 0);
    }

    private static GameModifierScoreFormulaSyntaxValidator.ModifierScoreFormulaContext CreateContext(
        int killsCount,
        int perKillBonus
    ) =>
        new(
            killsCount,
            BountyCount: 0,
            ScoreUnit: 100,
            BaseScore: 100,
            PerKillBonus: perKillBonus,
            FailurePenaltyPoints: 25,
            ActivationCount: 1,
            TotalOutcomeCount: killsCount
        );
}
