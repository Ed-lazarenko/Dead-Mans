using backend.Application.Features.GameRounds;
using backend.Domain.Persistence;

namespace Backend.Tests.Unit.Features.GameRounds;

public sealed class GameRoundScoreCalculatorTests
{
    [Theory]
    [InlineData(GameRoundStatusValue.AwaitingModifiers)]
    [InlineData(GameRoundStatusValue.InProgress)]
    [InlineData(GameRoundStatusValue.ReviewingResults)]
    [InlineData(GameRoundStatusValue.Cancelled)]
    public void Calculate_WhenRoundIsNotCompleted_ReturnsZeroScore(string status)
    {
        var result = GameRoundScoreCalculator.Calculate(
            new GameRoundScoreInput(
                status,
                BaseScore: 125,
                KillsCount: 3,
                BountyCount: 2,
                Modifiers: [new GameRoundScoreModifierInput(ScoreDelta: 45, KillDelta: 1)]
            )
        );

        Assert.Equal(125, result.ScoreUnit);
        Assert.Equal(0, result.FinalScore);
        Assert.Equal(0, result.PenaltyTotal);
        Assert.Equal(0, result.BonusDelta);
        Assert.Equal(0, result.TotalKillCount);
        Assert.False(result.EmptyCardPenaltyApplied);
    }

    [Fact]
    public void Calculate_WhenRoundHasEveryScoreSource_ReturnsDetailedBreakdown()
    {
        var result = GameRoundScoreCalculator.Calculate(
            new GameRoundScoreInput(
                GameRoundStatusValue.Completed,
                BaseScore: 120,
                KillsCount: 2,
                BountyCount: 1,
                Modifiers: [new GameRoundScoreModifierInput(ScoreDelta: 30, KillDelta: 1)]
            )
        );

        Assert.Equal(240, result.KillsScore);
        Assert.Equal(120, result.BountyScore);
        Assert.Equal(1, result.ModifierKillDelta);
        Assert.Equal(120, result.ModifierKillScore);
        Assert.Equal(30, result.ModifierScoreDelta);
        Assert.Equal(3, result.TotalKillCount);
        Assert.Equal(510, result.FinalScore);
        Assert.Equal(390, result.BonusDelta);
        Assert.Equal(0, result.PenaltyTotal);
    }

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

    [Fact]
    public void Calculate_WhenPositiveValuesOverflow_ClampsEveryPublishedValue()
    {
        var result = GameRoundScoreCalculator.Calculate(
            new GameRoundScoreInput(
                GameRoundStatusValue.Completed,
                BaseScore: int.MaxValue,
                KillsCount: int.MaxValue,
                BountyCount: int.MaxValue,
                Modifiers:
                [
                    new GameRoundScoreModifierInput(
                        ScoreDelta: int.MaxValue,
                        KillDelta: int.MaxValue
                    )
                ]
            )
        );

        Assert.Equal(int.MaxValue, result.KillsScore);
        Assert.Equal(int.MaxValue, result.BountyScore);
        Assert.Equal(int.MaxValue, result.ModifierKillScore);
        Assert.Equal(int.MaxValue, result.TotalKillCount);
        Assert.Equal(int.MaxValue, result.FinalScore);
        Assert.Equal(int.MaxValue, result.BonusDelta);
    }

    [Fact]
    public void Calculate_WhenPenaltyExceedsIntRange_ClampsWithoutThrowing()
    {
        var result = GameRoundScoreCalculator.Calculate(
            new GameRoundScoreInput(
                GameRoundStatusValue.Completed,
                BaseScore: 100,
                KillsCount: 0,
                BountyCount: 0,
                Modifiers:
                [
                    new GameRoundScoreModifierInput(
                        ScoreDelta: int.MinValue,
                        KillDelta: 0
                    )
                ]
            )
        );

        Assert.True(result.EmptyCardPenaltyApplied);
        Assert.Equal(int.MinValue, result.FinalScore);
        Assert.Equal(int.MaxValue, result.PenaltyTotal);
        Assert.Equal(int.MinValue, result.BonusDelta);
    }
}
