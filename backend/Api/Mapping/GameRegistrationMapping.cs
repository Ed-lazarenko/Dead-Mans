using ApiContracts = backend.Api.Contracts;
using AppContracts = backend.Application.Contracts;

namespace backend.Api.Mapping;

public static class GameRegistrationMapping
{
    public static ApiContracts.GameRegistrationSnapshotDto ToDto(
        this AppContracts.GameRegistrationSnapshot snapshot
    ) =>
        new(
            snapshot.GameId,
            snapshot.GameStatus,
            snapshot.MinPlayersPerTeam,
            snapshot.MaxPlayersPerTeam,
            snapshot.TeamSlots.Select(ToDto).ToArray(),
            snapshot.Teams.Select(ToDto).ToArray(),
            snapshot.MyTeam is null ? null : ToDto(snapshot.MyTeam),
            snapshot.MyPendingInvitations.Select(ToDto).ToArray(),
            snapshot.MyOutgoingInvitations.Select(ToDto).ToArray(),
            snapshot.CanInvitePlayersToMyTeam,
            snapshot.InvitablePlayers.Select(ToDto).ToArray()
        );

    public static ApiContracts.GameRegistrationAdminSnapshotDto ToDto(
        this AppContracts.GameRegistrationAdminSnapshot snapshot
    ) =>
        new(
            snapshot.GameId,
            snapshot.GameStatus,
            snapshot.MinPlayersPerTeam,
            snapshot.MaxPlayersPerTeam,
            snapshot.LaunchSummary.ToDto(),
            snapshot.TeamSlots.Select(ToDto).ToArray(),
            snapshot.Teams.Select(ToDto).ToArray(),
            snapshot.AvailablePlayers.Select(ToDto).ToArray()
        );

    public static ApiContracts.GameRegistrationLaunchSummaryDto ToDto(
        this AppContracts.GameRegistrationLaunchSummary summary
    ) =>
        new(
            summary.CanStartGame,
            summary.ConfirmedTeamsCount,
            summary.FormingTeamsCount,
            summary.PendingInvitationsCount,
            summary.DisbandRequestsCount,
            summary.InvalidConfirmedRostersCount
        );

    public static ApiContracts.RegistrationTeamDto ToDto(this AppContracts.RegistrationTeamDto team) =>
        new(
            team.TeamId,
            team.Name,
            team.TeamSlotIndex,
            team.TeamSlotType,
            team.ReservedLabel,
            team.RecruitmentOpen,
            team.Status,
            team.IsPlayed,
            team.DisbandRequestedAtUtc,
            team.DisbandRequestedByUserId,
            team.DisbandRequestedByDisplayName,
            team.IsActiveInGame,
            team.Members.Select(ToDto).ToArray(),
            team.PendingInvitations.Select(ToDto).ToArray()
        );

    public static ApiContracts.RegistrationInvitationDto ToDto(
        this AppContracts.RegistrationInvitationDto invitation
    ) =>
        new(
            invitation.InvitationId,
            invitation.TeamSlotId,
            invitation.TeamSlotIndex,
            invitation.TeamId,
            invitation.Status,
            invitation.CreatedAtUtc,
            invitation.InvitedByDisplayName,
            invitation.InvitedUserDisplayName
        );

    private static ApiContracts.RegistrationTeamMemberDto ToDto(
        AppContracts.RegistrationTeamMemberDto member
    ) =>
        new(ToDto(member.Player), member.JoinedAtUtc);

    private static ApiContracts.RegistrationTeamPendingInvitationDto ToDto(
        AppContracts.RegistrationTeamPendingInvitationDto invitation
    ) =>
        new(invitation.InvitationId, ToDto(invitation.Player), invitation.CreatedAtUtc);

    private static ApiContracts.RegistrationPlayerDto ToDto(AppContracts.RegistrationPlayerDto player) =>
        new(player.UserId, player.Login, player.DisplayName);

    private static ApiContracts.RegistrationTeamSlotDto ToDto(AppContracts.RegistrationTeamSlotDto slot) =>
        new(
            slot.TeamSlotId,
            slot.TeamSlotIndex,
            slot.TeamSlotType,
            slot.ReservedLabel,
            slot.IsAvailableForNewTeam,
            slot.TeamId,
            slot.TeamStatus
        );
}
