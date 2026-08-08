using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public interface IGameRegistrationPersistence
{
    Task<GameRegistrationResult<RegistrationTeamDto>> PersistCreateTeamAsync(
        Guid gameId,
        Guid userId,
        Guid teamSlotId,
        bool recruitmentOpen,
        string? name = null,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> PersistCreateEmptyTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamSlotId,
        bool recruitmentOpen,
        string? name = null,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> PersistUpdateTeamNameAsync(
        Guid gameId,
        Guid teamId,
        string? name,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> PersistJoinTeamAsync(
        Guid gameId,
        Guid userId,
        Guid teamId,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> PersistAssignPlayerAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> PersistRemovePlayerFromTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> PersistCancelTeamInvitationAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> PersistMoveTeamToSlotAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        Guid targetTeamSlotId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> PersistLeaveTeamAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> PersistRequestTeamDisbandAsync(
        Guid gameId,
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> PersistConfirmTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        short minPlayersPerTeam,
        short maxPlayersPerTeam,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> PersistRejectTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> PersistDisbandConfirmedTeamAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationInvitationDto>> PersistCreateAdminInvitationAsync(
        Guid gameId,
        Guid adminUserId,
        Guid teamSlotId,
        int teamSlotIndex,
        Guid invitedUserId,
        Guid? teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationInvitationDto>> PersistCreatePlayerInvitationAsync(
        Guid gameId,
        Guid userId,
        Guid teamSlotId,
        int teamSlotIndex,
        Guid invitedUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> PersistCancelPlayerInvitationAsync(
        Guid gameId,
        Guid userId,
        Guid teamId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> PersistAcceptInvitationAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> PersistDeclineInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    );
}
