using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Domain.Persistence;

namespace backend.Application.Features.GameRegistration;

public sealed partial class GameRegistrationService
{
    public async Task<GameRegistrationResult<RegistrationInvitationDto>> CreateAdminInvitationAsync(
        Guid adminUserId,
        Guid teamSlotId,
        Guid invitedUserId,
        Guid? teamId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        var slot = await _reads.GetTeamSlotAsync(game.GameId, teamSlotId, cancellationToken);
        if (slot is null)
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.SlotNotFound);
        }

        if (!await _reads.ActiveUserExistsAsync(invitedUserId, cancellationToken))
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.UserNotFound);
        }

        if (await _reads.UserHasTeamMembershipAsync(game.GameId, invitedUserId, cancellationToken))
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.UserAlreadyOnTeam);
        }

        if (await _reads.HasPendingInvitationAsync(game.GameId, invitedUserId, cancellationToken))
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.PendingInvitationExists);
        }

        if (!teamId.HasValue)
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.TeamInviteNotAllowed);
        }

        Guid? inviteTeamId = null;
        if (teamId.HasValue)
        {
            var team = await _reads.GetTeamInviteTargetSnapshotAsync(
                game.GameId,
                teamId.Value,
                cancellationToken
            );
            if (team is null)
            {
                return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.TeamNotFound);
            }

            if (team.TeamSlotId != slot.TeamSlotId || team.Status != TeamStatusValue.Forming)
            {
                return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.TeamNotJoinable);
            }

            if (team.RecruitmentOpen)
            {
                return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.TeamInviteNotAllowed);
            }

            if (team.MemberCount + team.PendingInvitationCount >= game.MaxPlayersPerTeam)
            {
                return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.TeamFull);
            }

            inviteTeamId = team.TeamId;
        }

        return await _persistence.PersistCreateAdminInvitationAsync(
            game.GameId,
            adminUserId,
            slot.TeamSlotId,
            slot.TeamSlotIndex,
            invitedUserId,
            inviteTeamId,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationInvitationDto>> CreatePlayerInvitationAsync(
        Guid userId,
        Guid invitedUserId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        var teamId = await _reads.GetActiveTeamIdForUserAsync(game.GameId, userId, cancellationToken);
        if (!teamId.HasValue)
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.NotTeamMember);
        }

        var team = await _reads.GetTeamInviteTargetSnapshotAsync(game.GameId, teamId.Value, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.CreatedByUserId != userId
            || team.RecruitmentOpen
            || team.Status != TeamStatusValue.Forming
            || team.MemberCount + team.PendingInvitationCount >= game.MaxPlayersPerTeam)
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.TeamInviteNotAllowed);
        }

        var slot = await _reads.GetTeamSlotAsync(game.GameId, team.TeamSlotId, cancellationToken);
        if (slot is null)
        {
            return Fail<RegistrationInvitationDto>(GameRegistrationErrorCode.SlotNotFound);
        }

        return await _persistence.PersistCreatePlayerInvitationAsync(
            game.GameId,
            userId,
            slot.TeamSlotId,
            slot.TeamSlotIndex,
            invitedUserId,
            team.TeamId,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> CancelPlayerInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.GameNotInReady);
        }

        var teamId = await _reads.GetActiveTeamIdForUserAsync(game.GameId, userId, cancellationToken);
        if (!teamId.HasValue)
        {
            return Fail<bool>(GameRegistrationErrorCode.NotTeamMember);
        }

        var team = await _reads.GetTeamInviteTargetSnapshotAsync(game.GameId, teamId.Value, cancellationToken);
        if (team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.CreatedByUserId != userId || team.RecruitmentOpen || team.Status != TeamStatusValue.Forming)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamInviteNotAllowed);
        }

        return await _persistence.PersistCancelPlayerInvitationAsync(
            game.GameId,
            userId,
            team.TeamId,
            invitationId,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> AcceptInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    )
    {
        var invitation = await _reads.GetPendingInvitationAsync(userId, invitationId, cancellationToken);
        if (invitation is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvitationNotFound);
        }

        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null || game.GameId != invitation.GameId)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        if (await _reads.UserHasTeamMembershipAsync(game.GameId, userId, cancellationToken))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.UserAlreadyOnTeam);
        }

        if (invitation.TeamId.HasValue)
        {
            var team = await _reads.GetTeamInviteTargetSnapshotAsync(
                game.GameId,
                invitation.TeamId.Value,
                cancellationToken
            );
            if (team is null)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
            }

            if (team.TeamSlotId != invitation.TeamSlotId || team.Status != TeamStatusValue.Forming)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
            }

            if (team.MemberCount >= game.MaxPlayersPerTeam)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamFull);
            }
        }
        else
        {
            var blockedTeamSlotIds = await _reads.GetBlockedTeamSlotIdsAsync(game.GameId, cancellationToken);
            if (IGameRegistrationReadStore.IsSlotBlocked(invitation.TeamSlotId, blockedTeamSlotIds))
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.SlotNotAvailable);
            }
        }

        return await _persistence.PersistAcceptInvitationAsync(
            new AcceptInvitationCommand(
                invitation.InvitationId,
                userId,
                game.GameId,
                invitation.TeamSlotId,
                invitation.TeamId,
                game.MaxPlayersPerTeam
            ),
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> DeclineInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    )
    {
        var invitation = await _reads.GetPendingInvitationAsync(userId, invitationId, cancellationToken);
        if (invitation is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.InvitationNotFound);
        }

        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null || game.GameId != invitation.GameId)
        {
            return Fail<bool>(GameRegistrationErrorCode.GameNotInReady);
        }

        return await _persistence.PersistDeclineInvitationAsync(userId, invitationId, cancellationToken);
    }
}
