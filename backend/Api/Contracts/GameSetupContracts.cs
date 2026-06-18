namespace backend.Api.Contracts;

public sealed record CreateGameSetupRequestDto(string Title);

public sealed record UpdateGameSetupCellDto(string? Id, int Row, int Col, string? Title, int Cost);

public sealed record UpdateGameSetupRequestDto(
    int ExpectedVersion,
    string Title,
    IReadOnlyList<string> RowLabels,
    IReadOnlyList<string> ColLabels,
    IReadOnlyList<UpdateGameSetupCellDto> Cells,
    IReadOnlyList<string>? EnabledModifierCodes = null,
    IReadOnlyList<string>? EnabledQuestionIds = null
);

public sealed record GameSetupSnapshotDto(
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
    IReadOnlyList<string> EnabledModifierCodes,
    IReadOnlyList<string> EnabledQuestionIds
);
