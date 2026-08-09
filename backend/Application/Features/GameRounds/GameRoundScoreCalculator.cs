using backend.Domain.Persistence;

namespace backend.Application.Features.GameRounds;

public static class GameRoundScoreCalculator
{
    public static GameRoundScoreBreakdown Calculate(GameRoundScoreInput input)
    {
        var normalizedStatus = input.Status.Trim().ToLowerInvariant();
        if (normalizedStatus == GameRoundStatusValue.Cancelled)
        {
            return new GameRoundScoreBreakdown(
                input.BaseScore,
                0,
                0,
                0,
                0,
                0,
                false,
                0,
                0,
                0,
                0,
                0
            );
        }

        var modifierKillDelta = input.Modifiers.Sum(x => x.KillDelta);
        var modifierScoreDelta = input.Modifiers.Sum(x => x.ScoreDelta);
        var totalKillCount = input.KillsCount + modifierKillDelta;
        var killsScore = input.KillsCount * input.BaseScore;
        var bountyScore = input.BountyCount * input.BaseScore;
        var modifierKillScore = modifierKillDelta * input.BaseScore;
        var cardOutcomeScore = killsScore + bountyScore + modifierKillScore;
        var emptyCardPenaltyApplied =
            input.BaseScore > 0 && cardOutcomeScore == 0 && modifierScoreDelta <= 0;
        var emptyCardPenaltyScore = emptyCardPenaltyApplied ? -1 * input.BaseScore : 0;

        var finalScore = cardOutcomeScore + modifierScoreDelta + emptyCardPenaltyScore;
        var bonusDelta = emptyCardPenaltyApplied ? finalScore : finalScore - input.BaseScore;

        return new GameRoundScoreBreakdown(
            input.BaseScore,
            killsScore,
            bountyScore,
            modifierKillDelta,
            modifierKillScore,
            modifierScoreDelta,
            emptyCardPenaltyApplied,
            emptyCardPenaltyScore,
            finalScore < 0 ? Math.Abs(finalScore) : 0,
            bonusDelta,
            totalKillCount,
            finalScore
        );
    }
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
