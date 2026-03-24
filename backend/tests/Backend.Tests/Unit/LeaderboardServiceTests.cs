using backend.Application.Abstractions.Repositories;
using backend.Application.Features.Leaderboard;
using backend.Domain.Models;

namespace Backend.Tests.Unit;

public sealed class LeaderboardServiceTests
{
    [Fact]
    public async Task GetLeaderboardAsync_OrdersByTotalScoreDescending()
    {
        var leader = new LeaderboardTeam { Id = "leader", Name = "Leader", ColorHex = "#111" };
        leader.ApplyDelta(20, 5);
        var runnerUp = new LeaderboardTeam { Id = "runner-up", Name = "Runner-up", ColorHex = "#222" };
        runnerUp.ApplyDelta(8, 0);
        var trailing = new LeaderboardTeam { Id = "trailing", Name = "Trailing", ColorHex = "#333" };
        trailing.ApplyDelta(2, 0);
        var teams = new List<LeaderboardTeam> { trailing, leader, runnerUp };
        var repository = new StubLeaderboardRepository(teams);
        var service = new LeaderboardService(repository);

        var summary = await service.GetLeaderboardAsync(CancellationToken.None);

        Assert.Equal(["leader", "runner-up", "trailing"], summary.Teams.Select(team => team.Id).ToArray());
    }

    private sealed class StubLeaderboardRepository : ILeaderboardRepository
    {
        private readonly IReadOnlyList<LeaderboardTeam> _teams;

        public StubLeaderboardRepository(IReadOnlyList<LeaderboardTeam> teams)
        {
            _teams = teams;
        }

        public Task<IReadOnlyList<LeaderboardTeam>> GetTeamsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_teams);
        }
    }
}
