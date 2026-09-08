using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class GameRegistrationReadStore
{
    public async Task<GameRegistrationSnapshot> BuildSnapshotAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .FirstAsync(x => x.Id == gameId && !x.IsDeleted, cancellationToken);

        var slots = await _dbContext.GameTeamSlots
            .AsNoTracking()
            .Where(slot => slot.GameId == gameId)
            .OrderBy(slot => slot.SlotIndex)
            .ToListAsync(cancellationToken);

        var teams = await _dbContext.GameTeams
            .AsNoTracking()
            .Where(
                team => team.GameId == gameId
                    && (team.Status == TeamStatusValue.Forming || team.Status == TeamStatusValue.Confirmed)
            )
            .ToListAsync(cancellationToken);

        var teamDtos = await LoadTeamsDtoAsync(gameId, cancellationToken);
        var blockedSlotIds = await GetBlockedTeamSlotIdsAsync(gameId, cancellationToken);

        var myTeam = teamDtos.FirstOrDefault(
            team => team.Members.Any(member => member.Player.UserId == userId)
        );

        var slotDtos = new List<RegistrationTeamSlotDto>();
        foreach (var slot in slots)
        {
            var occupyingTeam = teams.FirstOrDefault(
                team => team.SlotId == slot.Id && TeamStatusValue.OccupiesSlot(team.Status)
            );
            var blocked = IGameRegistrationReadStore.IsSlotBlocked(slot.Id, blockedSlotIds);
            slotDtos.Add(
                new RegistrationTeamSlotDto(
                    slot.Id,
                    slot.SlotIndex,
                    slot.SlotType,
                    slot.ReservedLabel,
                    slot.SlotType == TeamSlotTypeValue.Public
                        && !blocked
                        && occupyingTeam is null,
                    occupyingTeam?.Id,
                    occupyingTeam?.Status
                )
            );
        }

        var myInvites = await (
            from invitation in _dbContext.GameTeamInvitations.AsNoTracking()
            join slot in _dbContext.GameTeamSlots.AsNoTracking() on invitation.SlotId equals slot.Id
            join invitedByUser in _dbContext.Users.AsNoTracking() on invitation.InvitedByUserId equals invitedByUser.Id into invitedByUsers
            from invitedByUser in invitedByUsers.DefaultIfEmpty()
            join invitedUser in _dbContext.Users.AsNoTracking() on invitation.InvitedUserId equals invitedUser.Id into invitedUsers
            from invitedUser in invitedUsers.DefaultIfEmpty()
            where invitation.GameId == gameId
                && invitation.InvitedUserId == userId
                && invitation.Status == TeamInvitationStatusValue.Pending
            select new RegistrationInvitationDto(
                invitation.Id,
                slot.Id,
                slot.SlotIndex,
                invitation.TeamId,
                invitation.Status,
                invitation.CreatedAtUtc,
                invitedByUser != null ? invitedByUser.DisplayName : null,
                invitedUser != null ? invitedUser.DisplayName : null
            )
        ).ToListAsync(cancellationToken);

        var myTeamEntity = myTeam is null
            ? null
            : await _dbContext.GameTeams
                .AsNoTracking()
                .Where(team => team.Id == myTeam.TeamId)
                .Select(team => new { team.Id, team.CreatedByUserId, team.RecruitmentOpen, team.Status })
                .FirstOrDefaultAsync(cancellationToken);

        List<RegistrationInvitationDto> myOutgoingInvitations = myTeamEntity is null
            ? []
            : await (
                from invitation in _dbContext.GameTeamInvitations.AsNoTracking()
                join slot in _dbContext.GameTeamSlots.AsNoTracking() on invitation.SlotId equals slot.Id
                join invitedByUser in _dbContext.Users.AsNoTracking() on invitation.InvitedByUserId equals invitedByUser.Id into invitedByUsers
                from invitedByUser in invitedByUsers.DefaultIfEmpty()
                join invitedUser in _dbContext.Users.AsNoTracking() on invitation.InvitedUserId equals invitedUser.Id into invitedUsers
                from invitedUser in invitedUsers.DefaultIfEmpty()
                where invitation.GameId == gameId
                    && invitation.TeamId == myTeamEntity.Id
                    && invitation.Status == TeamInvitationStatusValue.Pending
                select new RegistrationInvitationDto(
                    invitation.Id,
                    slot.Id,
                    slot.SlotIndex,
                    invitation.TeamId,
                    invitation.Status,
                    invitation.CreatedAtUtc,
                    invitedByUser != null ? invitedByUser.DisplayName : null,
                    invitedUser != null ? invitedUser.DisplayName : null
                )
            ).ToListAsync(cancellationToken);

        var canInvitePlayersToMyTeam = myTeamEntity is not null
            && myTeamEntity.CreatedByUserId == userId
            && !myTeamEntity.RecruitmentOpen
            && myTeamEntity.Status == TeamStatusValue.Forming
            && (myTeam?.Members.Count ?? 0) < game.MaxPlayersPerTeam
            && myOutgoingInvitations.Count == 0;

        IReadOnlyList<RegistrationPlayerDto> invitablePlayers = canInvitePlayersToMyTeam
            ? await _dbContext.Users
                .AvailableForGameRegistration(_dbContext, gameId, excludedUserId: userId)
                .Select(user => new RegistrationPlayerDto(user.Id, user.Login, user.DisplayName))
                .ToListAsync(cancellationToken)
            : [];

        return new GameRegistrationSnapshot(
            gameId,
            game.Status,
            game.MinPlayersPerTeam,
            game.MaxPlayersPerTeam,
            slotDtos,
            teamDtos,
            myTeam,
            myInvites,
            myOutgoingInvitations,
            canInvitePlayersToMyTeam,
            invitablePlayers
        );
    }

    public async Task<GameRegistrationAdminSnapshot> BuildAdminSnapshotAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _dbContext.Games
            .AsNoTracking()
            .FirstAsync(x => x.Id == gameId && !x.IsDeleted, cancellationToken);

        var slotDtos = await BuildSlotDtosAsync(gameId, cancellationToken);
        var teamDtos = await LoadTeamsDtoAsync(gameId, cancellationToken);
        var availablePlayers = await _dbContext.Users
            .AvailableForGameRegistration(_dbContext, gameId)
            .Select(user => new RegistrationPlayerDto(user.Id, user.Login, user.DisplayName))
            .ToListAsync(cancellationToken);

        return new GameRegistrationAdminSnapshot(
            gameId,
            game.Status,
            game.MinPlayersPerTeam,
            game.MaxPlayersPerTeam,
            BuildLaunchSummary(teamDtos, game.MinPlayersPerTeam, game.MaxPlayersPerTeam),
            slotDtos,
            teamDtos,
            availablePlayers
        );
    }

    private static GameRegistrationLaunchSummary BuildLaunchSummary(
        IReadOnlyList<RegistrationTeamDto> teams,
        short minPlayersPerTeam,
        short maxPlayersPerTeam
    )
    {
        var confirmedTeamsCount = teams.Count(team => team.Status == TeamStatusValue.Confirmed);
        var formingTeamsCount = teams.Count(team => team.Status == TeamStatusValue.Forming);
        var pendingInvitationsCount = teams.Sum(team => team.PendingInvitations.Count);
        var disbandRequestsCount = teams.Count(team => team.DisbandRequestedAtUtc is not null);
        var invalidConfirmedRostersCount = teams.Count(
            team =>
                team.Status == TeamStatusValue.Confirmed
                && (team.Members.Count < minPlayersPerTeam || team.Members.Count > maxPlayersPerTeam)
        );

        return new GameRegistrationLaunchSummary(
            confirmedTeamsCount > 0
            && formingTeamsCount == 0
            && pendingInvitationsCount == 0
            && disbandRequestsCount == 0
            && invalidConfirmedRostersCount == 0,
            confirmedTeamsCount,
            formingTeamsCount,
            pendingInvitationsCount,
            disbandRequestsCount,
            invalidConfirmedRostersCount
        );
    }
}
