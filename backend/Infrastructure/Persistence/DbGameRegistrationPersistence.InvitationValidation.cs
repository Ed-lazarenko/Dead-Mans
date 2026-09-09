using backend.Application.Contracts;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRegistrationPersistence
{
    private async Task<GameRegistrationErrorCode> ValidateInvitationTargetAsync(
        Guid gameId,
        Guid slotId,
        Guid invitedUserId,
        Guid? teamId,
        CancellationToken cancellationToken
    )
    {
        var invitedUserExistsAndActive = await _dbContext.Users.AnyAsync(
            user => user.Id == invitedUserId && user.IsActive,
            cancellationToken
        );
        if (!invitedUserExistsAndActive)
        {
            return GameRegistrationErrorCode.UserNotFound;
        }

        var slotExists = await _dbContext.GameTeamSlots.AnyAsync(
            slot => slot.Id == slotId && slot.GameId == gameId,
            cancellationToken
        );
        if (!slotExists)
        {
            return GameRegistrationErrorCode.SlotNotFound;
        }

        var userAlreadyOnTeam = await (
            from member in _dbContext.GameTeamMembers
            join memberTeam in _dbContext.GameTeams on member.TeamId equals memberTeam.Id
            where member.GameId == gameId
                && member.UserId == invitedUserId
                && member.LeftAtUtc == null
                && (memberTeam.Status == TeamStatusValue.Forming
                    || memberTeam.Status == TeamStatusValue.Confirmed)
            select member.Id
        ).AnyAsync(cancellationToken);
        if (userAlreadyOnTeam)
        {
            return GameRegistrationErrorCode.UserAlreadyOnTeam;
        }

        var hasPendingInvitationForUser = await _dbContext.GameTeamInvitations.AnyAsync(
            invitation =>
                invitation.GameId == gameId
                && invitation.InvitedUserId == invitedUserId
                && invitation.Status == TeamInvitationStatusValue.Pending,
            cancellationToken
        );
        if (hasPendingInvitationForUser)
        {
            return GameRegistrationErrorCode.PendingInvitationExists;
        }

        if (!teamId.HasValue)
        {
            return GameRegistrationErrorCode.TeamInviteNotAllowed;
        }

        var team = await _dbContext.GameTeams
            .Where(candidate => candidate.Id == teamId.Value && candidate.GameId == gameId)
            .Select(candidate => new { candidate.SlotId, candidate.Status, candidate.RecruitmentOpen })
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
        {
            return GameRegistrationErrorCode.TeamNotFound;
        }

        if (team.SlotId != slotId || team.Status != TeamStatusValue.Forming)
        {
            return GameRegistrationErrorCode.TeamNotJoinable;
        }

        if (team.RecruitmentOpen)
        {
            return GameRegistrationErrorCode.TeamInviteNotAllowed;
        }

        var maxPlayersPerTeam = await _dbContext.Games
            .Where(game => game.Id == gameId)
            .Select(game => game.MaxPlayersPerTeam)
            .FirstAsync(cancellationToken);
        var activeMemberCount = await _dbContext.GameTeamMembers.CountAsync(
            member => member.TeamId == teamId.Value && member.LeftAtUtc == null,
            cancellationToken
        );
        var pendingInvitationCount = await _dbContext.GameTeamInvitations.CountAsync(
            invitation =>
                invitation.TeamId == teamId.Value
                && invitation.Status == TeamInvitationStatusValue.Pending,
            cancellationToken
        );
        if (activeMemberCount + pendingInvitationCount >= maxPlayersPerTeam)
        {
            return GameRegistrationErrorCode.TeamFull;
        }

        return GameRegistrationErrorCode.None;
    }
}
