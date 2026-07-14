namespace backend.Api.Contracts;

public sealed record RegistrationPlayerDto(Guid UserId, string Login, string DisplayName);

public sealed record RegistrationTeamMemberDto(RegistrationPlayerDto Player, DateTime JoinedAtUtc);

public sealed record RegistrationTeamDto(
    Guid TeamId,
    int SlotIndex,
    string SlotAvailability,
    string? ReservedLabel,
    bool RecruitmentOpen,
    string Status,
    IReadOnlyList<RegistrationTeamMemberDto> Members
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

public sealed record GameRegistrationAdminSnapshotDto(
    Guid GameId,
    string GameStatus,
    int MinPlayersPerTeam,
    int MaxPlayersPerTeam,
    IReadOnlyList<RegistrationSlotDto> Slots,
    IReadOnlyList<RegistrationTeamDto> Teams,
    IReadOnlyList<RegistrationPlayerDto> AvailablePlayers
);

public sealed record GameRegistrationSnapshotDto(
    Guid GameId,
    string GameStatus,
    int MinPlayersPerTeam,
    int MaxPlayersPerTeam,
    IReadOnlyList<RegistrationSlotDto> Slots,
    IReadOnlyList<RegistrationTeamDto> Teams,
    RegistrationTeamDto? MyTeam,
    IReadOnlyList<RegistrationInvitationDto> MyPendingInvitations,
    IReadOnlyList<RegistrationInvitationDto> MyOutgoingInvitations,
    bool CanInvitePlayersToMyTeam,
    IReadOnlyList<RegistrationPlayerDto> InvitablePlayers
);

public sealed record CreateRegistrationTeamRequestDto(bool RecruitmentOpen);

public sealed record CreateAdminRegistrationTeamRequestDto(Guid? SlotId, bool RecruitmentOpen);

public sealed record AssignRegistrationPlayerRequestDto(Guid UserId);

public sealed record MoveRegistrationTeamRequestDto(Guid TargetSlotId);

public sealed record CreateAdminInvitationRequestDto(
    Guid SlotId,
    Guid InvitedUserId,
    Guid? TeamId
);

public sealed record CreatePlayerInvitationRequestDto(Guid InvitedUserId);

public sealed record GameLifecycleStateDto(Guid GameId, string Status);
