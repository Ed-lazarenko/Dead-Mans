namespace backend.Api.Contracts;

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

public sealed record GameRegistrationAdminSnapshotDto(
    Guid GameId,
    string GameStatus,
    int MinPlayersPerTeam,
    int MaxPlayersPerTeam,
    IReadOnlyList<RegistrationTeamSlotDto> TeamSlots,
    IReadOnlyList<RegistrationTeamDto> Teams,
    IReadOnlyList<RegistrationPlayerDto> AvailablePlayers
);

public sealed record GameRegistrationSnapshotDto(
    Guid GameId,
    string GameStatus,
    int MinPlayersPerTeam,
    int MaxPlayersPerTeam,
    IReadOnlyList<RegistrationTeamSlotDto> TeamSlots,
    IReadOnlyList<RegistrationTeamDto> Teams,
    RegistrationTeamDto? MyTeam,
    IReadOnlyList<RegistrationInvitationDto> MyPendingInvitations,
    IReadOnlyList<RegistrationInvitationDto> MyOutgoingInvitations,
    bool CanInvitePlayersToMyTeam,
    IReadOnlyList<RegistrationPlayerDto> InvitablePlayers
);

public sealed record CreateRegistrationTeamRequestDto(bool RecruitmentOpen, string? Name = null);

public sealed record CreateAdminRegistrationTeamRequestDto(
    Guid? TeamSlotId,
    bool RecruitmentOpen,
    string? Name = null
);

public sealed record UpdateRegistrationTeamNameRequestDto(string? Name);

public sealed record AssignRegistrationPlayerRequestDto(Guid UserId);

public sealed record MoveRegistrationTeamRequestDto(Guid TargetTeamSlotId);

public sealed record CreateAdminInvitationRequestDto(
    Guid TeamSlotId,
    Guid InvitedUserId,
    Guid? TeamId
);

public sealed record CreatePlayerInvitationRequestDto(Guid InvitedUserId);

public sealed record GameLifecycleStateDto(Guid GameId, string Status);
