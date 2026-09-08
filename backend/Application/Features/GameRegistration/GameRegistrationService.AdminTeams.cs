using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Domain.Persistence;

namespace backend.Application.Features.GameRegistration;

public sealed partial class GameRegistrationService
{
    public async Task<GameRegistrationResult<RegistrationTeamDto>> CreateEmptyTeamAsync(
        Guid adminUserId,
        Guid? teamSlotId,
        bool recruitmentOpen,
        string? name = null,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = TeamNameValue.Normalize(name);
        if (!TeamNameValue.IsValid(normalizedName))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvalidTeamName);
        }

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
            normalizedName,
            cancellationToken
        );
    }

    public async Task<GameRegistrationResult<RegistrationTeamDto>> UpdateTeamNameAsync(
        Guid teamId,
        string? name,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedName = TeamNameValue.Normalize(name);
        if (!TeamNameValue.IsValid(normalizedName))
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.InvalidTeamName);
        }

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

        return await _persistence.PersistUpdateTeamNameAsync(
            game.GameId,
            teamId,
            normalizedName,
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
}
