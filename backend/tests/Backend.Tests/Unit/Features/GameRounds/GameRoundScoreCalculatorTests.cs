using backend.Application.Features.GameRounds;
using backend.Domain.Persistence;

namespace Backend.Tests.Unit.Features.GameRounds;

public sealed class GameRoundScoreCalculatorTests
{
    [Fact]
    public void Calculate_WhenCardHasNoScoredOutcome_UsesCardCostAsPenalty()
    {
        var result = GameRoundScoreCalculator.Calculate(
            new GameRoundScoreInput(
                GameRoundStatusValue.Completed,
                BaseScore: 125,
                KillsCount: 0,
                BountyCount: 0,
                Modifiers: []
            )
        );

        Assert.True(result.EmptyCardPenaltyApplied);
        Assert.Equal(-125, result.EmptyCardPenaltyScore);
        Assert.Equal(-125, result.FinalScore);
    }

    [Fact]
    public void Calculate_WhenEmptyCardHasModifierPenalties_AddsCardPenaltyToModifierPenalties()
    {
        var result = GameRoundScoreCalculator.Calculate(
            new GameRoundScoreInput(
                GameRoundStatusValue.Completed,
                BaseScore: 100,
                KillsCount: 0,
                BountyCount: 0,
                Modifiers:
                [
                    new GameRoundScoreModifierInput(ScoreDelta: -25, KillDelta: 0),
                    new GameRoundScoreModifierInput(ScoreDelta: -25, KillDelta: 0)
                ]
            )
        );

        Assert.True(result.EmptyCardPenaltyApplied);
        Assert.Equal(-100, result.EmptyCardPenaltyScore);
        Assert.Equal(-50, result.ModifierScoreDelta);
        Assert.Equal(-150, result.FinalScore);
    }

    [Fact]
    public void Calculate_WhenModifierGrantsPositivePoints_DoesNotApplyEmptyCardPenalty()
    {
        var result = GameRoundScoreCalculator.Calculate(
            new GameRoundScoreInput(
                GameRoundStatusValue.Completed,
                BaseScore: 100,
                KillsCount: 0,
                BountyCount: 0,
                Modifiers: [new GameRoundScoreModifierInput(ScoreDelta: 45, KillDelta: 0)]
            )
        );

        Assert.False(result.EmptyCardPenaltyApplied);
        Assert.Equal(45, result.FinalScore);
    }
}
