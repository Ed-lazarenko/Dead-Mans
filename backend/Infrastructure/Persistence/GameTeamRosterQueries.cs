using backend.Data;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

internal sealed record GameTeamRosterProjection(
    Guid TeamId,
    int TeamSlotIndex,
    IReadOnlyList<GameTeamParticipantProjection> Participants
);

internal sealed record GameTeamParticipantProjection(Guid UserId, string DisplayName);

internal static class GameTeamRosterQueries
{
    public static async Task<IReadOnlyList<GameTeamRosterProjection>> LoadConfirmedTeamRostersAsync(
        this ApplicationDbContext dbContext,
        Guid gameId,
        CancellationToken cancellationToken
    )
    {
        var teams = await dbContext.GameTeams
            .AsNoTracking()
            .Where(
                team =>
                    team.GameId == gameId
                    && team.Status == TeamStatusValue.Confirmed
                    && team.DisbandedAtUtc == null
            )
            .OrderBy(team => team.Slot!.SlotIndex)
            .Select(
                team =>
                    new
                    {
                        team.Id,
                        TeamSlotIndex = team.Slot != null ? team.Slot.SlotIndex : 0,
                    }
            )
            .ToArrayAsync(cancellationToken);

        if (teams.Length == 0)
        {
            return Array.Empty<GameTeamRosterProjection>();
        }

        var teamIds = teams.Select(team => team.Id).ToArray();
        var participants = await dbContext.GameTeamMembers
            .AsNoTracking()
            .Where(
                member =>
                    member.GameId == gameId
                    && member.LeftAtUtc == null
                    && teamIds.Contains(member.TeamId)
            )
            .OrderBy(member => member.JoinedAtUtc)
            .Select(
                member =>
                    new
                    {
                        member.TeamId,
                        Participant = new GameTeamParticipantProjection(
                            member.UserId,
                            member.User != null && !string.IsNullOrWhiteSpace(member.User.DisplayName)
                                ? member.User.DisplayName
                                : member.UserId.ToString()
                        )
                    }
            )
            .ToArrayAsync(cancellationToken);

        var participantsByTeamId = participants
            .GroupBy(participant => participant.TeamId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<GameTeamParticipantProjection>)
                    group.Select(participant => participant.Participant).ToArray()
            );

        return teams
            .Select(
                team =>
                    new GameTeamRosterProjection(
                        team.Id,
                        team.TeamSlotIndex,
                        participantsByTeamId.GetValueOrDefault(
                            team.Id,
                            Array.Empty<GameTeamParticipantProjection>()
                        )
                    )
            )
            .Where(team => team.Participants.Count > 0)
            .ToArray();
    }
}
