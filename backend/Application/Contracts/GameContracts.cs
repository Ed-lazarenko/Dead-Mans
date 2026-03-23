using backend.Domain.Models;

namespace backend.Application.Contracts;

public sealed record LeaderboardTeam(
    string Id,
    string Name,
    string ColorHex,
    int Score,
    int Penalty
);

public sealed record LeaderboardSummary(
    DateTimeOffset UpdatedAt,
    IReadOnlyList<LeaderboardTeam> Teams
);

public sealed record LoadoutCell(
    string Id,
    int Row,
    int Col,
    string Label,
    int Points,
    string? ImageUrl,
    LoadoutCellState State
);

public sealed record LoadoutBoard(
    int Rows,
    int Cols,
    IReadOnlyList<string> RowLabels,
    IReadOnlyList<string> ColLabels,
    IReadOnlyList<LoadoutCell> Cells
);

public sealed record ModifierDefinition(
    string Id,
    string Name,
    int Cost,
    string Description
);

public sealed record ActiveModifier(
    string Id,
    string ModifierId,
    DateTimeOffset ActivatedAt,
    string TriggeredBy
);

public sealed record ModifiersSnapshot(
    IReadOnlyList<ModifierDefinition> Available,
    IReadOnlyList<ActiveModifier> Active
);

public sealed record ActivateModifierCommand(string ModifierId, string TriggeredBy);

public sealed record GameControlState(
    GamePhase Phase,
    int CurrentRound,
    int TotalRounds,
    DateTimeOffset? LastActionAt
);
