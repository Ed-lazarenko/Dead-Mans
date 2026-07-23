namespace backend.Application.Contracts;

public sealed record ReadyGameRegistrationContext(
    Guid GameId,
    short MinPlayersPerTeam,
    short MaxPlayersPerTeam
);

public sealed record AvailableParticipationSlot(Guid SlotId, int SlotIndex);

public sealed record ParticipationSlotSnapshot(Guid SlotId, int SlotIndex);

public sealed record JoinableTeamSnapshot(Guid TeamId, string Status, bool RecruitmentOpen);

public sealed record TeamAdminActionSnapshot(string Status, int MemberCount);

public sealed record TeamAdminLifecycleSnapshot(
    string Status,
    int MemberCount,
    bool IsActiveInGame
);

public sealed record TeamInviteTargetSnapshot(
    Guid TeamId,
    Guid SlotId,
    string Status,
    int MemberCount,
    int PendingInvitationCount,
    bool RecruitmentOpen,
    Guid? CreatedByUserId
);

public sealed record PendingInvitationSnapshot(
    Guid InvitationId,
    Guid GameId,
    Guid SlotId,
    Guid? TeamId,
    string Status,
    Guid InvitedUserId
);

public sealed record AcceptInvitationCommand(
    Guid InvitationId,
    Guid UserId,
    Guid GameId,
    Guid SlotId,
    Guid? TeamId,
    short MaxPlayersPerTeam
);

public sealed record RegistrationPlayerDto(Guid UserId, string Login, string DisplayName);

public sealed record RegistrationTeamMemberDto(RegistrationPlayerDto Player, DateTime JoinedAtUtc);

public sealed record RegistrationTeamPendingInvitationDto(
    Guid InvitationId,
    RegistrationPlayerDto Player,
    DateTime CreatedAtUtc
);

public sealed record RegistrationTeamDto(
    Guid TeamId,
    int SlotIndex,
    string SlotAvailability,
    string? ReservedLabel,
    bool RecruitmentOpen,
    string Status,
    bool IsPlayed,
    DateTime? DisbandRequestedAtUtc,
    Guid? DisbandRequestedByUserId,
    string? DisbandRequestedByDisplayName,
    bool IsActiveInGame,
    IReadOnlyList<RegistrationTeamMemberDto> Members,
    IReadOnlyList<RegistrationTeamPendingInvitationDto> PendingInvitations
);

public sealed record RegistrationSlotDto(
    Guid SlotId,
    int SlotIndex,
    string Availability,
    string? ReservedLabel,
    bool IsAvailableForNewTeam,
    Guid? TeamId,
    string? TeamStatus
);

public sealed record RegistrationInvitationDto(
    Guid InvitationId,
    Guid SlotId,
    int SlotIndex,
    Guid? TeamId,
    string Status,
    DateTime CreatedAtUtc,
    string? InvitedByDisplayName,
    string? InvitedUserDisplayName
);

public sealed record GameRegistrationSnapshot(
    Guid GameId,
    string GameStatus,
    short MinPlayersPerTeam,
    short MaxPlayersPerTeam,
    IReadOnlyList<RegistrationSlotDto> Slots,
    IReadOnlyList<RegistrationTeamDto> Teams,
    RegistrationTeamDto? MyTeam,
    IReadOnlyList<RegistrationInvitationDto> MyPendingInvitations,
    IReadOnlyList<RegistrationInvitationDto> MyOutgoingInvitations,
    bool CanInvitePlayersToMyTeam,
    IReadOnlyList<RegistrationPlayerDto> InvitablePlayers
);

public sealed record GameRegistrationAdminSnapshot(
    Guid GameId,
    string GameStatus,
    short MinPlayersPerTeam,
    short MaxPlayersPerTeam,
    IReadOnlyList<RegistrationSlotDto> Slots,
    IReadOnlyList<RegistrationTeamDto> Teams,
    IReadOnlyList<RegistrationPlayerDto> AvailablePlayers
);

public enum GameRegistrationErrorCode
{
    None,
    GameNotInReady,
    UserAlreadyOnTeam,
    NoAvailableSlot,
    TeamNotFound,
    TeamNotJoinable,
    TeamFull,
    NotTeamMember,
    InvitationNotFound,
    InvitationNotPending,
    UserNotFound,
    SlotNotFound,
    SlotNotAvailable,
    PendingInvitationExists,
    PendingOutgoingInvitation,
    TeamInviteNotAllowed,
    TargetTeamSameAsSource,
    TeamActiveInGame,
    OperationFailed,
}

public sealed record GameRegistrationResult<T>(bool Success, T? Value, GameRegistrationErrorCode Error);
