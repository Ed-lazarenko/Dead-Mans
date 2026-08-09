namespace backend.Api.Contracts;

public sealed record GameBoardCellMediaDto(string Url);

public sealed record GameBoardCellDto(
    string Id,
    int Row,
    int Col,
    string CellType,
    string? Title,
    string? Description,
    int Cost,
    string State,
    IReadOnlyList<GameBoardCellMediaDto> Media
);

public sealed record GameBoardSnapshotDto(
    string GameId,
    string Title,
    string? Description,
    string Status,
    int Version,
    int Rows,
    int Cols,
    IReadOnlyList<string> RowLabels,
    IReadOnlyList<string> ColLabels,
    IReadOnlyList<GameBoardCellDto> Cells,
    IReadOnlyList<string> EnabledModifierIds,
    IReadOnlyList<GameModifierActivationDto> ActiveModifiers,
    string? ActiveTeamId
);

public sealed record SetActiveGameTeamRequestDto(string? TeamId);

public sealed record SetGameTeamPlayedStateRequestDto(bool IsPlayed);

public sealed record GameTeamQueueParticipantDto(string UserId, string DisplayName);

public sealed record GameTeamQueueItemDto(
    string TeamId,
    string? TeamName,
    int TeamSlotIndex,
    bool IsPlayed,
    DateTime? PlayedAtUtc,
    IReadOnlyList<GameTeamQueueParticipantDto> Participants
);

public sealed record GameTeamQueueSummaryDto(
    int TotalTeams,
    int PlayedTeams,
    int RemainingTeams
);

public sealed record GameTeamQueueResultDto(
    GameTeamQueueSummaryDto Summary,
    IReadOnlyList<GameTeamQueueItemDto> Teams
);
