namespace backend.Application.Contracts;

public sealed record ReadyGameRegistrationContext(
    Guid GameId,
    short MinPlayersPerTeam,
    short MaxPlayersPerTeam
);

public sealed record AvailableTeamSlot(Guid TeamSlotId, int TeamSlotIndex);

public sealed record TeamSlotSnapshot(Guid TeamSlotId, int TeamSlotIndex);

public sealed record JoinableTeamSnapshot(Guid TeamId, string Status, bool RecruitmentOpen);

public sealed record TeamAdminActionSnapshot(string Status, int MemberCount);

public sealed record TeamAdminLifecycleSnapshot(
    string Status,
    int MemberCount,
    bool IsActiveInGame
);

public sealed record TeamInviteTargetSnapshot(
    Guid TeamId,
    Guid TeamSlotId,
    string Status,
    int MemberCount,
    int PendingInvitationCount,
    bool RecruitmentOpen,
    Guid? CreatedByUserId
);

public sealed record PendingInvitationSnapshot(
    Guid InvitationId,
    Guid GameId,
    Guid TeamSlotId,
    Guid? TeamId,
    string Status,
    Guid InvitedUserId
);

public sealed record AcceptInvitationCommand(
    Guid InvitationId,
    Guid UserId,
    Guid GameId,
    Guid TeamSlotId,
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
    string? Name,
    int TeamSlotIndex,
    string TeamSlotType,
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

public sealed record RegistrationTeamSlotDto(
    Guid TeamSlotId,
    int TeamSlotIndex,
    string TeamSlotType,
    string? ReservedLabel,
    bool IsAvailableForNewTeam,
    Guid? TeamId,
    string? TeamStatus
);

public sealed record RegistrationInvitationDto(
    Guid InvitationId,
    Guid TeamSlotId,
    int TeamSlotIndex,
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
    IReadOnlyList<RegistrationTeamSlotDto> TeamSlots,
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
    IReadOnlyList<RegistrationTeamSlotDto> TeamSlots,
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
    InvalidTeamName,
    OperationFailed,
}

public sealed record GameRegistrationResult<T>(bool Success, T? Value, GameRegistrationErrorCode Error);
