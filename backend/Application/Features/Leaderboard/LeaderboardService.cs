using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;

namespace backend.Application.Features.Leaderboard;

public sealed class LeaderboardService : ILeaderboardService
{
    private readonly ILeaderboardRepository _repository;

    public LeaderboardService(ILeaderboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<LeaderboardSummary> GetLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        var teams = await _repository.GetTeamsAsync(cancellationToken);

        var orderedTeams = teams
            .OrderByDescending(team => team.TotalScore)
            .Select(team => new Contracts.LeaderboardTeam(
                team.Id,
                team.Name,
                team.ColorHex,
                team.Score,
                team.Penalty
            ))
            .ToArray();

        return new LeaderboardSummary(DateTimeOffset.UtcNow, orderedTeams);
    }
}
