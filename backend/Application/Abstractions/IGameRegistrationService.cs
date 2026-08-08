using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface IGameRegistrationService
{
    Task<GameRegistrationSnapshot?> GetRegistrationSnapshotAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> CreateTeamAsync(
        Guid userId,
        bool recruitmentOpen,
        string? name = null,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> UpdateMyTeamNameAsync(
        Guid userId,
        string? name,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> JoinTeamAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> LeaveTeamAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> RequestMyTeamDisbandAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<RegistrationTeamDto>?> ListTeamsAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationAdminSnapshot?> GetAdminSnapshotAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> CreateEmptyTeamAsync(
        Guid adminUserId,
        Guid? teamSlotId,
        bool recruitmentOpen,
        string? name = null,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> UpdateTeamNameAsync(
        Guid teamId,
        string? name,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> AssignPlayerAsync(
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> RemovePlayerFromTeamAsync(
        Guid adminUserId,
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> CancelTeamInvitationAsync(
        Guid adminUserId,
        Guid teamId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> MoveTeamToSlotAsync(
        Guid adminUserId,
        Guid teamId,
        Guid targetTeamSlotId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> ConfirmTeamAsync(
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> RejectTeamAsync(
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> DisbandConfirmedTeamAsync(
        Guid adminUserId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationInvitationDto>> CreateAdminInvitationAsync(
        Guid adminUserId,
        Guid teamSlotId,
        Guid invitedUserId,
        Guid? teamId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationInvitationDto>> CreatePlayerInvitationAsync(
        Guid userId,
        Guid invitedUserId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> CancelPlayerInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<RegistrationTeamDto>> AcceptInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationResult<bool>> DeclineInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    );
}
