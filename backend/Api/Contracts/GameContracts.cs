namespace backend.Api.Contracts;

public sealed record HealthStatusResponse(string Status, string Environment, DateTimeOffset ServerTimeUtc);

public sealed record LeaderboardTeamDto(
    string Id,
    string Name,
    string ColorHex,
    int Score,
    int Penalty
);

public sealed record LeaderboardSummaryDto(
    DateTimeOffset UpdatedAt,
    IReadOnlyList<LeaderboardTeamDto> Teams
);

public sealed record LoadoutCellDto(
    string Id,
    int Row,
    int Col,
    string Label,
    int Points,
    string? ImageUrl,
    string State
);

public sealed record LoadoutBoardDto(
    int Rows,
    int Cols,
    IReadOnlyList<string> RowLabels,
    IReadOnlyList<string> ColLabels,
    IReadOnlyList<LoadoutCellDto> Cells
);

public sealed record ModifierDefinitionDto(
    string Id,
    string Name,
    int Cost,
    string Description
);

public sealed record ActiveModifierDto(
    string Id,
    string ModifierId,
    DateTimeOffset ActivatedAt,
    string TriggeredBy
);

public sealed record ModifiersSnapshotDto(
    IReadOnlyList<ModifierDefinitionDto> Available,
    IReadOnlyList<ActiveModifierDto> Active
);

public sealed record ActivateModifierRequest(string ModifierId, string TriggeredBy);

public sealed record GameControlStateDto(
    string Phase,
    int CurrentRound,
    int TotalRounds,
    DateTimeOffset? LastActionAt
);
