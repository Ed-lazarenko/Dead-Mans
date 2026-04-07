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
    IReadOnlyList<GameBoardCellDto> Cells
);
