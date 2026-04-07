using backend.Domain.Models;

namespace backend.Application.Contracts;

public sealed record GameBoardCellMedia(string Url);

public sealed record GameBoardCell(
    string Id,
    int Row,
    int Col,
    string CellType,
    string? Title,
    string? Description,
    int Cost,
    GameBoardCellState State,
    IReadOnlyList<GameBoardCellMedia> Media
);

public sealed record GameBoardSnapshot(
    string GameId,
    string Title,
    string? Description,
    string Status,
    int Version,
    int Rows,
    int Cols,
    IReadOnlyList<string> RowLabels,
    IReadOnlyList<string> ColLabels,
    IReadOnlyList<GameBoardCell> Cells
);

public sealed record OpenGameCellResult(
    string GameId,
    int Version,
    GameBoardCell Cell,
    bool StateChanged
);

public sealed record GameCellOpenedEvent(
    string GameId,
    int Version,
    GameBoardCell Cell
);
