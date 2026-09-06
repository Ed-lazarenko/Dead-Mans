using backend.Application.Features.Scoring;

namespace Backend.Tests.Unit.Application;

public sealed class GameTeamResultCalculatorTests
{
    [Fact]
    public void Calculate_UsesBestPrePenaltyScoreAndAllPenalties()
    {
        var teamId = Guid.NewGuid();
        var result = Assert.Single(GameTeamResultCalculator.Calculate([
            Team(teamId,
                Round(finalScore: 90, penalty: 10, bonus: 5, kills: 2),
                Round(finalScore: 115, penalty: 0, bonus: 15, kills: 3),
                Round(finalScore: -100, penalty: 100, bonus: -100, kills: 0))
        ]));

        Assert.Equal(115, result.BestScore);
        Assert.Equal(110, result.PenaltyTotal);
        Assert.Equal(5, result.FinalScore);
        Assert.Equal(105, result.TotalScore);
        Assert.Equal(-80, result.TotalBonusDelta);
        Assert.Equal(5, result.TotalKills);
    }

    [Fact]
    public void Calculate_AssignsCompetitionPlacementsAndKeepsUnplayedNullable()
    {
        var tiedFirstA = Team(Guid.NewGuid(), Round(finalScore: 100));
        var tiedFirstB = Team(Guid.NewGuid(), Round(finalScore: 100));
        var third = Team(Guid.NewGuid(), Round(finalScore: 90));
        var unplayed = Team(Guid.NewGuid());

        var result = GameTeamResultCalculator.Calculate([
            unplayed,
            third,
            tiedFirstB,
            tiedFirstA
        ]);

        Assert.Equal([1, 1, 3], result.Where(x => x.FinalScore.HasValue).Select(x => x.Placement));
        var noRounds = Assert.Single(result, x => x.TeamId == unplayed.TeamId);
        Assert.Null(noRounds.BestScore);
        Assert.Null(noRounds.FinalScore);
        Assert.Null(noRounds.Placement);
    }

    [Fact]
    public void Calculate_SaturatesAllAggregateNumericBoundaries()
    {
        var result = Assert.Single(GameTeamResultCalculator.Calculate([
            Team(
                Guid.NewGuid(),
                Round(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue),
                Round(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue))
        ]));

        Assert.Equal(int.MaxValue, result.BestScore);
        Assert.Equal(int.MaxValue, result.PenaltyTotal);
        Assert.Equal(0, result.FinalScore);
        Assert.Equal(int.MaxValue, result.TotalScore);
        Assert.Equal(int.MaxValue, result.TotalKills);
    }

    private static GameTeamResultCalculationInput Team(
        Guid teamId,
        params GameTeamRoundScoreFact[] rounds
    ) => new(teamId, null, 1, [], rounds);

    private static GameTeamRoundScoreFact Round(
        int finalScore,
        int penalty = 0,
        int bonus = 0,
        int kills = 0
    ) => new(Guid.NewGuid(), finalScore, penalty, bonus, kills, 0, DateTime.UtcNow);
}
