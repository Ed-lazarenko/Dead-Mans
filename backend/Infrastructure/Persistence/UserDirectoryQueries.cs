using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

internal static class UserDirectoryQueries
{
    public static IOrderedQueryable<User> ActiveUsersByDisplayName(this IQueryable<User> users) =>
        users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Login);

    public static IQueryable<User> AvailableForGameRegistration(
        this IQueryable<User> users,
        ApplicationDbContext dbContext,
        Guid gameId,
        Guid? excludedUserId = null
    )
    {
        var activeRosterUserIds =
            from member in dbContext.GameTeamMembers.AsNoTracking()
            join team in dbContext.GameTeams.AsNoTracking() on member.TeamId equals team.Id
            where member.GameId == gameId
                && member.LeftAtUtc == null
                && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            select member.UserId;

        var pendingInvitationUserIds = dbContext.GameTeamInvitations
            .AsNoTracking()
            .Where(
                invitation =>
                    invitation.GameId == gameId
                    && invitation.Status == TeamInvitationStatusValue.Pending
            )
            .Select(invitation => invitation.InvitedUserId);

        var query = users
            .ActiveUsersByDisplayName()
            .Where(
                user =>
                    !activeRosterUserIds.Contains(user.Id)
                    && !pendingInvitationUserIds.Contains(user.Id)
            );

        return excludedUserId.HasValue
            ? query.Where(user => user.Id != excludedUserId.Value)
            : query;
    }
}
