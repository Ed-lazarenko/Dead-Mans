using backend.Application.Contracts;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRoundRepository
{
    public async Task<IReadOnlyList<GameRoundTeamOption>> GetEligibleTeamsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeGameId = await _dbContext.Games
            .AsNoTracking()
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .OrderByDescending(x => x.StartedAtUtc ?? x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!activeGameId.HasValue)
        {
            return Array.Empty<GameRoundTeamOption>();
        }

        var rosters = await _dbContext.LoadConfirmedTeamRostersAsync(
            activeGameId.Value,
            cancellationToken
        );
        if (rosters.Count == 0)
        {
            return Array.Empty<GameRoundTeamOption>();
        }

        return rosters
            .Select(roster =>
                new GameRoundTeamOption(
                    roster.TeamId,
                    roster.TeamName,
                    roster.TeamSlotIndex,
                    roster.Participants
                        .Select(participant => new GameRoundParticipantSnapshot(
                            participant.UserId,
                            participant.DisplayName
                        ))
                        .ToArray()
                )
            )
            .ToArray();
    }

    public async Task<GameRoundDetails?> GetActiveAsync(
        CancellationToken cancellationToken = default
    )
    {
        var activeRoundId = await _dbContext.GameRounds
            .AsNoTracking()
            .Where(x => !x.Game.IsDeleted
                && x.Game.Status == GameStatusValue.Active
                && ActiveRoundStatuses.Contains(x.Status))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!activeRoundId.HasValue)
        {
            return null;
        }

        return await LoadRoundDetailsAsync(activeRoundId.Value, cancellationToken);
    }
}
