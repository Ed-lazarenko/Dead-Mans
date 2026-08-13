using backend.Domain.Persistence;
using backend.Application.Features.Scoring;

namespace backend.Application.Features.GameRounds;

public static class GameRoundScoreCalculator
{
    public static GameRoundScoreBreakdown Calculate(GameRoundScoreInput input)
    {
        var normalizedStatus = input.Status.Trim().ToLowerInvariant();
        if (normalizedStatus != GameRoundStatusValue.Completed)
        {
            return EmptyBreakdown(input.BaseScore);
        }

        var rawModifierKillDelta = input.Modifiers.Sum(x => (decimal)x.KillDelta);
        var rawModifierScoreDelta = input.Modifiers.Sum(x => (decimal)x.ScoreDelta);
        var rawTotalKillCount = input.KillsCount + rawModifierKillDelta;
        var rawKillsScore = (decimal)input.KillsCount * input.BaseScore;
        var rawBountyScore = (decimal)input.BountyCount * input.BaseScore;
        var rawModifierKillScore = rawModifierKillDelta * input.BaseScore;
        var rawCardOutcomeScore = rawKillsScore + rawBountyScore + rawModifierKillScore;
        var emptyCardPenaltyApplied =
            input.BaseScore > 0 && rawCardOutcomeScore == 0 && rawModifierScoreDelta <= 0;
        var rawEmptyCardPenaltyScore = emptyCardPenaltyApplied ? -1m * input.BaseScore : 0m;

        var rawFinalScore = rawCardOutcomeScore + rawModifierScoreDelta + rawEmptyCardPenaltyScore;
        var rawBonusDelta = emptyCardPenaltyApplied ? rawFinalScore : rawFinalScore - input.BaseScore;

        return new GameRoundScoreBreakdown(
            input.BaseScore,
            SaturatingInt32.From(rawKillsScore),
            SaturatingInt32.From(rawBountyScore),
            SaturatingInt32.From(rawModifierKillDelta),
            SaturatingInt32.From(rawModifierKillScore),
            SaturatingInt32.From(rawModifierScoreDelta),
            emptyCardPenaltyApplied,
            SaturatingInt32.From(rawEmptyCardPenaltyScore),
            rawFinalScore < 0 ? SaturatingInt32.From(decimal.Abs(rawFinalScore)) : 0,
            SaturatingInt32.From(rawBonusDelta),
            SaturatingInt32.From(rawTotalKillCount),
            SaturatingInt32.From(rawFinalScore)
        );
    }

    private static GameRoundScoreBreakdown EmptyBreakdown(int scoreUnit) =>
        new(scoreUnit, 0, 0, 0, 0, 0, false, 0, 0, 0, 0, 0);

}

public sealed record GameRoundScoreInput(
    string Status,
    int BaseScore,
    int KillsCount,
    int BountyCount,
    IReadOnlyList<GameRoundScoreModifierInput> Modifiers
);

public sealed record GameRoundScoreModifierInput(int ScoreDelta, int KillDelta);

public sealed record GameRoundScoreBreakdown(
    int ScoreUnit,
    int KillsScore,
    int BountyScore,
    int ModifierKillDelta,
    int ModifierKillScore,
    int ModifierScoreDelta,
    bool EmptyCardPenaltyApplied,
    int EmptyCardPenaltyScore,
    int PenaltyTotal,
    int BonusDelta,
    int TotalKillCount,
    int FinalScore
);
