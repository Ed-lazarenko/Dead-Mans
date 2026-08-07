using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Domain.Persistence;

namespace backend.Application.Features.GameRegistration;

public sealed class GameRegistrationService : IGameRegistrationService
{
    private readonly IGameRegistrationReadStore _reads;
    private readonly IGameRegistrationPersistence _persistence;

    public GameRegistrationService(
        IGameRegistrationReadStore reads,
        IGameRegistrationPersistence persistence
    )
    {
        _reads = reads;
        _persistence = persistence;
    }

    public async Task<GameRegistrationSnapshot?> GetRegistrationSnapshotAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return null;
        }

        return await _reads.BuildSnapshotAsync(game.GameId, userId, cancellationToken);
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> CreateTeamAsync(
        Guid userId,
        bool recruitmentOpen,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        if (await _reads.UserHasTeamMembershipAsync(game.GameId, userId, cancellationToken))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.UserAlreadyOnTeam);
        }

        var slot = await _reads.FindAvailablePublicSlotAsync(game.GameId, cancellationToken);
        if (slot is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.NoAvailableSlot);
        }

        return await _persistence.PersistCreateTeamAsync(
            game.GameId,
            userId,
            slot.TeamSlotId,
            recruitmentOpen,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> JoinTeamAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        if (await _reads.UserHasTeamMembershipAsync(game.GameId, userId, cancellationToken))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.UserAlreadyOnTeam);
        }

        var team = await _reads.GetJoinableTeamAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming || !team.RecruitmentOpen)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistJoinTeamAsync(
            game.GameId,
            userId,
            teamId,
            game.MaxPlayersPerTeam,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> LeaveTeamAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.GameNotInReady);
        }

        var activeTeamId = await _reads.GetActiveTeamIdForUserAsync(game.GameId, userId, cancellationToken);
        if (activeTeamId.HasValue)
        {
            var team = await _reads.GetTeamAdminActionSnapshotAsync(
                game.GameId,
                activeTeamId.Value,
                cancellationToken
            );
            if (team?.Status == TeamStatusValue.Confirmed)
            {
                return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
            }
        }

        if (activeTeamId.HasValue
            && await _reads.TeamHasPendingInvitationAsync(game.GameId, activeTeamId.Value, cancellationToken))
        {
            return Fail<bool>(GameRegistrationErrorCode.PendingOutgoingInvitation);
        }

        return await _persistence.PersistLeaveTeamAsync(game.GameId, userId, cancellationToken);
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> RequestMyTeamDisbandAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetReadyGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        var activeTeamId = await _reads.GetActiveTeamIdForUserAsync(game.GameId, userId, cancellationToken);
        if (!activeTeamId.HasValue)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.NotTeamMember);
        }

        var team = await _reads.GetTeamAdminActionSnapshotAsync(
            game.GameId,
            activeTeamId.Value,
            cancellationToken
        );
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistRequestTeamDisbandAsync(
            game.GameId,
            userId,
            activeTeamId.Value,
            cancellationToken
        );
    }

    public async Task<IReadOnlyList<RegistrationTeamDto>?> ListTeamsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return null;
        }

        return await _reads.LoadTeamsForGameAsync(game.GameId, cancellationToken);
    }

    public async Task<GameRegistrationAdminSnapshot?> GetAdminSnapshotAsync(
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return null;
        }

        return await _reads.BuildAdminSnapshotAsync(game.GameId, cancellationToken);
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> CreateEmptyTeamAsync(
        Guid adminUserId,
        Guid? teamSlotId,
        bool recruitmentOpen,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        Guid resolvedTeamSlotId;
        if (teamSlotId.HasValue)
        {
            var slot = await _reads.GetTeamSlotAsync(game.GameId, teamSlotId.Value, cancellationToken);
            if (slot is null)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.SlotNotFound);
            }

            var blockedTeamSlotIds = await _reads.GetBlockedTeamSlotIdsAsync(game.GameId, cancellationToken);
            if (IGameRegistrationReadStore.IsSlotBlocked(slot.TeamSlotId, blockedTeamSlotIds))
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.SlotNotAvailable);
            }

            resolvedTeamSlotId = slot.TeamSlotId;
        }
        else
        {
            var slot = await _reads.FindAvailablePublicSlotAsync(game.GameId, cancellationToken);
            if (slot is null)
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.NoAvailableSlot);
            }

            resolvedTeamSlotId = slot.TeamSlotId;
        }

        return await _persistence.PersistCreateEmptyTeamAsync(
            game.GameId,
            adminUserId,
            resolvedTeamSlotId,
            recruitmentOpen,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> AssignPlayerAsync(
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        var team = await _reads.GetTeamInviteTargetSnapshotAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming && team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        if (!await _reads.ActiveUserExistsAsync(userId, cancellationToken))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.UserNotFound);
        }

        var sourceTeamId = await _reads.GetActiveTeamIdForUserAsync(game.GameId, userId, cancellationToken);
        if (sourceTeamId.HasValue && sourceTeamId.Value == teamId)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TargetTeamSameAsSource);
        }

        return await _persistence.PersistAssignPlayerAsync(
            game.GameId,
            adminUserId,
            teamId,
            userId,
            game.MaxPlayersPerTeam,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> RemovePlayerFromTeamAsync(
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.GameNotInReady);
        }

        var team = await _reads.GetTeamInviteTargetSnapshotAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming && team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistRemovePlayerFromTeamAsync(
            game.GameId,
            adminUserId,
            teamId,
            userId,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> CancelTeamInvitationAsync(
        Guid adminUserId,
        Guid teamId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.GameNotInReady);
        }

        var team = await _reads.GetTeamInviteTargetSnapshotAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming && team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistCancelTeamInvitationAsync(
            game.GameId,
            adminUserId,
            teamId,
            invitationId,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> MoveTeamToSlotAsync(
        Guid adminUserId,
        Guid teamId,
        Guid targetTeamSlotId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        var team = await _reads.GetTeamInviteTargetSnapshotAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming && team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        var slot = await _reads.GetTeamSlotAsync(game.GameId, targetTeamSlotId, cancellationToken);
        if (slot is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.SlotNotFound);
        }

        if (slot.TeamSlotId == team.TeamSlotId)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.SlotNotAvailable);
        }

        var targetOccupyingTeam = await _reads.GetTeamBySlotAsync(game.GameId, targetTeamSlotId, cancellationToken);
        if (targetOccupyingTeam is null)
        {
            var blockedTeamSlotIds = await _reads.GetBlockedTeamSlotIdsAsync(game.GameId, cancellationToken);
            if (IGameRegistrationReadStore.IsSlotBlocked(targetTeamSlotId, blockedTeamSlotIds))
            {
                return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.SlotNotAvailable);
            }
        }

        return await _persistence.PersistMoveTeamToSlotAsync(
            game.GameId,
            adminUserId,
            teamId,
            targetTeamSlotId,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> ConfirmTeamAsync(
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.GameNotInReady);
        }

        var team = await _reads.GetTeamAdminActionSnapshotAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        if (team.MemberCount < game.MinPlayersPerTeam || team.MemberCount > game.MaxPlayersPerTeam)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        if (await _reads.TeamHasPendingInvitationAsync(game.GameId, teamId, cancellationToken))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.PendingOutgoingInvitation);
        }

        return await _persistence.PersistConfirmTeamAsync(
            game.GameId,
            adminUserId,
            teamId,
            game.MinPlayersPerTeam,
            game.MaxPlayersPerTeam,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<bool>> RejectTeamAsync(
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.GameNotInReady);
        }

        var team = await _reads.GetTeamAdminActionSnapshotAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.Status != TeamStatusValue.Forming)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistRejectTeamAsync(game.GameId, adminUserId, teamId, cancellationToken);
    }

    public async Task<GameRegistrationResult<bool>> DisbandConfirmedTeamAsync(
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    )
    {
        var game = await _reads.GetManageableGameAsync(cancellationToken);
        if (game is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.GameNotInReady);
        }

        var team = await _reads.GetTeamAdminLifecycleSnapshotAsync(game.GameId, teamId, cancellationToken);
        if (team is null)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotFound);
        }

        if (team.IsActiveInGame)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamActiveInGame);
        }

        if (team.Status != TeamStatusValue.Confirmed)
        {
            return Fail<bool>(GameRegistrationErrorCode.TeamNotJoinable);
        }

        return await _persistence.PersistDisbandConfirmedTeamAsync(
            game.GameId,
            adminUserId,
            teamId,
            cancellationToken
        );
    }

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

    private static GameRegistrationResult<T> Fail<T>(GameRegistrationErrorCode error) =>
        new(false, default, error);
}
