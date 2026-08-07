using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public interface IGameRegistrationReadStore
{
    Task<ReadyGameRegistrationContext?> GetReadyGameAsync(CancellationToken cancellationToken = default);

    Task<ReadyGameRegistrationContext?> GetManageableGameAsync(
        CancellationToken cancellationToken = default
    );

    Task<bool> UserHasTeamMembershipAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasPendingInvitationAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<PendingInvitationSnapshot?> GetPendingInvitationAsync(
        Guid userId,
        Guid invitationId,
        CancellationToken cancellationToken = default
    );

    Task<AvailableTeamSlot?> FindAvailablePublicSlotAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<HashSet<Guid>> GetBlockedSlotIdsAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationSnapshot> BuildSnapshotAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<GameRegistrationAdminSnapshot> BuildAdminSnapshotAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<RegistrationTeamDto?> LoadTeamDtoAsync(
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<RegistrationTeamDto>> LoadTeamsForGameAsync(
        Guid gameId,
        CancellationToken cancellationToken = default
    );

    Task<JoinableTeamSnapshot?> GetJoinableTeamAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<TeamAdminActionSnapshot?> GetTeamAdminActionSnapshotAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<TeamAdminLifecycleSnapshot?> GetTeamAdminLifecycleSnapshotAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<TeamInviteTargetSnapshot?> GetTeamInviteTargetSnapshotAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    Task<Guid?> GetActiveTeamIdForUserAsync(
        Guid gameId,
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<TeamInviteTargetSnapshot?> GetTeamBySlotAsync(
        Guid gameId,
        Guid slotId,
        CancellationToken cancellationToken = default
    );

    Task<TeamSlotSnapshot?> GetTeamSlotAsync(
        Guid gameId,
        Guid slotId,
        CancellationToken cancellationToken = default
    );

    Task<bool> ActiveUserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> TeamHasPendingInvitationAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken = default
    );

    static bool IsSlotBlocked(Guid slotId, HashSet<Guid> blockedSlotIds) =>
        blockedSlotIds.Contains(slotId);
}
